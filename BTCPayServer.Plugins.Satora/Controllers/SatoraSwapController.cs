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
    }
}
