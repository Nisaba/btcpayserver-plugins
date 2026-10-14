using BTCPayServer.Client.Models;
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
            var invoice = await pluginService.GetInvoiceAsync(req.BtcPayInvoiceId);
            if (invoice == null || invoice.StoreId != storeId)
            {
                return new SwapCreationResponse
                {
                    Success = false,
                    StatusMessage = "Invalid invoice"
                };
            }

            if (invoice.Status != InvoiceStatus.New)
            {
                return new SwapCreationResponse
                {
                    Success = false,
                    StatusMessage = "Invoice already processed"
                };
            }
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

                var readToken = ExolixPluginService.GenerateReadToken();
                await pluginService.AddStoreTransaction(new ExolixTx
                {
                    StoreId = storeId,
                    AltcoinFrom = req.CryptoFrom,
                    DateT = DateTime.UtcNow,
                    BTCAmount = req.BtcAmount,
                    TxID = rep.SwapId,
                    BTCPayInvoiceId = req.BtcPayInvoiceId,
                    ReadTokenHash = ExolixPluginService.HashToken(readToken)
                });
                rep.ReadToken = readToken;

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
        public async Task<IActionResult> GetSwapInfo([FromRoute] string id, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return NotFound(new { error = "Swap not found" });
            }
            if (!await pluginService.ValidateReadTokenAsync(id, token))
            {
                return NotFound(new { error = "Swap not found" });
            }

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
