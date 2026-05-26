using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Plugins.Satora.Models;
using BTCPayServer.Plugins.Satora.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServer.Plugins.Satora.Controllers
{
    [Route("~/plugins/{storeId}/Satora")]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanModifyStoreSettings)]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanViewInvoices)]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanCreateNonApprovedPullPayments)]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanManagePayouts)]
    [AutoValidateAntiforgeryToken]
    public class SatoraPluginController(SatoraPluginService pluginService) : Controller
    {

        [HttpGet]
        public async Task<IActionResult> Index([FromRoute] string storeId)
        {
            var model = await pluginService.GetStoreData(storeId);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(SatoraSettings settings)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await pluginService.UpdateSettings(settings);
                    TempData[WellKnownTempData.SuccessMessage] = "Settings successfuly saved";
                }
                catch (Exception ex)
                {
                    TempData[WellKnownTempData.ErrorMessage] = $"Error: {ex.Message}";
                    throw;
                }
            }
            return RedirectToAction("Index");
        }


        [HttpGet]
        [Route("GetSwapStatus")]
        public async Task<ActionResult> GetSwapStatus(string swapId)
        {
            try
            {
                var status = await pluginService.DoGetSwapStatus(swapId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("swap/{swapId}")]
        public async Task<IActionResult> SwapDetails([FromRoute] string storeId, [FromRoute] string swapId)
        {
            var model = await pluginService.GetSwapDetailsAsync(storeId, swapId);
            return View(model);
        }

    }
}
