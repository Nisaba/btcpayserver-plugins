using BTCPayServer.Plugins.B2PCentral.Models;
using BTCPayServer.Plugins.B2PCentral.Models.Swaps;
using BTCPayServer.Plugins.B2PCentral.Services;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Ocsp;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.B2PCentral.Controllers
{
    [Route("~/plugins/{storeId}/B2PCentralCheckoutSwap")]
    public class B2PCentralSwapController(B2PCentralService b2pCentralService, B2PCentralPluginService pluginService) : Controller
    {

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<SwapCheckoutCreationResponse> Index([FromRoute] string storeId, [FromForm] SwapCheckoutCreationRequestJS swapReq)
        {
            var createdSwap = new SwapCheckoutCreationResponse();
            try
            {
                var quoteReq = new SwapRateRequest
                {
                    FiatCurrency = string.Empty,
                    FromAmount = 0,
                    FromCrypto = swapReq.FromCrypto.Split("-")[0],
                    FromNetwork = SwapCryptos.GetNetwork(swapReq.FromCrypto),
                    ToAmount = swapReq.ToAmount,
                    ToCrypto = swapReq.ToCrypto,
                    ToNetwork = swapReq.ToNetwork,
                    Providers = [swapReq.Provider]
                };

                var quoteResult = await b2pCentralService.GetSwapsListAsync(quoteReq, swapReq.ApiKey);

                var swapCreateReq = new SwapCreationRequest
                {
                    Provider = swapReq.Provider,
                    QuoteID = quoteResult.First().FixedQuoteId,
                    ToCrypto = swapReq.ToCrypto,
                    FromAmount = (decimal)quoteResult.First().FromFixedAmount,
                    ToAmount = swapReq.ToAmount,
                    ToAddress = swapReq.ToAddress,
                    FromRefundAddress = "",
                    IsFixed = swapReq.IsFixed,
                    NotificationEmail = swapReq.NotificationEmail,
                    FromCrypto = quoteReq.FromCrypto,
                    FromNetwork = quoteReq.FromNetwork,
                    ToNetwork = swapReq.ToNetwork,
                    NotificationNpub = ""
                };

                var swapResponse = await b2pCentralService.CreateSwapAsync(swapCreateReq, swapReq.ApiKey);
                createdSwap = new SwapCheckoutCreationResponse
                {
                    SwapId = swapResponse.SwapId,
                    Success = swapResponse.Success,
                    StatusMessage = swapResponse.StatusMessage,
                    FollowUrl = swapResponse.FollowUrl,
                    ProviderUrl = swapResponse.ProviderUrl,
                    FromAddress = swapResponse.FromAddress,
                    TransactionHash = swapResponse.TransactionHash,
                    ProviderName = SwapProviders.GetDisplayName(swapReq.Provider)
                };

                await pluginService.AddSwapInDb(new B2PStoreSwap
                {
                    StoreId = storeId,
                    DateT = DateTime.UtcNow,
                    Provider = swapReq.Provider,
                    SwapId = createdSwap.SwapId,
                    FollowUrl = createdSwap.FollowUrl,
                    ProviderUrl = createdSwap.ProviderUrl,
                    FromAmount = swapReq.FromAmount,
                    ToAmount = swapReq.ToAmount,
                    ToCrypto = swapReq.ToCrypto,
                    ToNetwork = swapReq.ToNetwork,
                    BTCPayPullPaymentId = swapReq.InvoiceId,
                    BTCPayPayoutId = string.Empty,
                    IsAutoSwap = false,
                    IsCheckoutSwap = true
                }); ;
                createdSwap.Success = true;
            }
            catch (Exception ex)
            {
                createdSwap.Success = false;
                createdSwap.StatusMessage = ex.Message;
            }

            return createdSwap;
        }
    }
}
