using BTCPayServer.Plugins.Exolix.Model;
using BTCPayServer.Plugins.Exolix.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Exolix.Controllers
{
    [Route("~/plugins/{storeId}/ExolixSwap")]
    public class ExolixSwapController(ExolixService exolixService, ExolixPluginService pluginService) : Controller
    {
        private readonly ExolixService _exolixService = exolixService;
        private readonly ExolixPluginService _PluginService = pluginService;

        [HttpPost]
        public async Task<SwapCreationResponse> Index([FromRoute] string storeId, [FromForm] SwapRequest req)
        {
            var rep = new SwapCreationResponse();
            try
            {
                string sCryptoFrom, sNetworkFrom;
                if (req.CryptoFrom.Contains("-"))
                {
                    var sSplit = req.CryptoFrom.Split('-');
                    sCryptoFrom = sSplit[0];
                    sNetworkFrom = sSplit[1];
                }
                else
                {
                    sCryptoFrom = req.CryptoFrom;
                    sNetworkFrom = req.CryptoFrom;
                }

                var exoliwSwapReq = new SwapCreationRequest
                {
                    FromCrypto = sCryptoFrom,
                    FromNetwork = sNetworkFrom,
                    FromAmount = 0,
                    ToCrypto = "BTC",
                    ToNetwork = "BTC",
                    ToAmount = req.BtcAmount,
                    ToAddress = req.BtcAddress,
                };
                rep = await _exolixService.CreateSwapAsync(exoliwSwapReq);

                await _PluginService.AddStoreTransaction(new ExolixTx
                {
                    StoreId = storeId,
                    AltcoinFrom = req.CryptoFrom,
                    DateT = DateTime.UtcNow,
                    BTCAmount = req.BtcAmount,
                    TxID = rep.SwapId,
                    BTCPayInvoiceId = req.BtcPayInvoiceId
                });
                rep.Success = true;
            }
            catch (Exception ex)
            {
                rep.Success = false;
                rep.StatusMessage = ex.Message;
            }

            return rep;
        }
    }
}
