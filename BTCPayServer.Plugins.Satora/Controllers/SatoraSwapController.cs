using BTCPayServer.Plugins.Satora.Models;
using BTCPayServer.Plugins.Satora.Services;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServer.Plugins.Satora.Controllers
{
    [Route("~/plugins/{storeId}/SatoraSwap")]
    public class SatoraSwapController(SatoraService satoraService, SatoraPluginService pluginService) : Controller
    {

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<SwapResponse> Index([FromRoute] string storeId, [FromForm] SwapRequest req)
        {
            var rep = new SwapResponse();
            try
            {
                rep = await satoraService.CreateSwapAsync(req);

                await pluginService.AddStoreTransaction(new SatoraTx
                {
                    StoreId = storeId,
                    Blockchain = req.NetworkFrom.ToString(),
                    Stablecoin = req.CryptoFrom.ToString(),
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
        public async Task<string> GetSwapInfo([FromRoute] string id)
        {
            return await satoraService.GetSwapInfoAsync(id);
        }

        // Manual recovery hatch: drives a single swap one step forward
        // based on its current backend status. Same dispatcher the
        // background watcher will eventually use.
        [HttpPost("{id}/continue")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Continue(
            [FromRoute] string storeId,
            [FromRoute] string id,
            [FromForm] string? destination)
        {
            try
            {
                // Empty string from a form input == "use the derived one".
                var dest = string.IsNullOrWhiteSpace(destination) ? null : destination;
                var (action, status) = await pluginService.ContinueSwapAsync(id, dest);
                return Json(new { ok = true, action, status });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }
    }
}
