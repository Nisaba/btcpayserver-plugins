using BTCPayServer.Plugins.Exolix.Model;
using BTCPayServer.Plugins.Exolix.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Exolix.Controllers
{
    [Route("~/plugins/{storeId}/ExolixSwap")]
    public class ExolixSwapController(ExolixService exolixService, ExolixPluginService pluginService) : Controller
    {

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<SwapCreationResponse> Index([FromRoute] string storeId, [FromForm] SwapRequest req, [FromQuery] bool isTrueNetwork = false)
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
                    ToNetwork = req.BtcNetwork,
                    ToAmount = req.BtcAmount,
                    ToAddress = req.BtcAddress,
                };
                rep = await exolixService.CreateSwapAsync(exoliwSwapReq, isTrueNetwork);

                await pluginService.AddStoreTransaction(new ExolixTx
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSwapInfo([FromRoute] string id)
        {
            try
            {
                var swapInfo = await exolixService.GetSwapInfoAsync(id);
                return Content(swapInfo, "application/json");
            }
            catch (Exception ex)
            {
                return Content($"{{\"error\":\"{ex.Message}\"}}", "application/json");
            }
        }
    }
}
