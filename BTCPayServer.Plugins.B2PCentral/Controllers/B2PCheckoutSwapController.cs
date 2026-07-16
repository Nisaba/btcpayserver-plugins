using BTCPayServer.Plugins.B2PCentral.Migrations;
using BTCPayServer.Plugins.B2PCentral.Models;
using BTCPayServer.Plugins.B2PCentral.Models.Swaps;
using BTCPayServer.Plugins.B2PCentral.Services;
using BTCPayServer.Storage.Models;
using Microsoft.AspNetCore.Mvc;
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
                    FromAmount = swapReq.FromAmount,
                    FromCrypto = swapReq.FromCrypto,
                    FromNetwork = swapReq.FromNetwork,
                    ToAmount = 0,
                    ToCrypto = swapReq.ToCrypto,
                    ToNetwork = swapReq.ToNetwork,
                    Providers = [swapReq.Provider]
                };

                var quoteResult = await b2pCentralService.GetSwapsListAsync(quoteReq, swapReq.ApiKey);
                swapReq.QuoteID = quoteResult.First().FixedQuoteId;

                var swapResponse = await b2pCentralService.CreateSwapAsync(swapReq, swapReq.ApiKey);

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
