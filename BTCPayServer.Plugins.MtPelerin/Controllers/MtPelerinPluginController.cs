using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Plugins.MtPelerin.Model;
using BTCPayServer.Plugins.MtPelerin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.MtPelerin.Controllers
{
    [Route("~/plugins/{storeId}/MtPelerin")]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanModifyStoreSettings)]
    [Authorize(Policy = Policies.CanCreateNonApprovedPullPayments, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [Authorize(Policy = Policies.CanManagePayouts, AuthenticationSchemes = AuthenticationSchemes.Cookie)]


    public class MtPelerinPluginController(MtPelerinPluginService pluginService) : Controller
    {
        private readonly MtPelerinPluginService _pluginService = pluginService;

        [HttpGet]
        public async Task<IActionResult> Index([FromRoute] string storeId)
        {
            var model = await _pluginService.GetStoreSettings(storeId);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(MtPelerinSettings model, string command)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (command == "save")
            {
                try
                {
                    await _pluginService.UpdateSettings(model);
                    TempData[WellKnownTempData.SuccessMessage] = "Settings successfuly saved";
                }
                catch (Exception ex)
                {
                    TempData[WellKnownTempData.ErrorMessage] = $"Error: {ex.Message}";
                }
           }
            return RedirectToAction("Index");
        }

        [HttpPost("createpayout")]
        public async Task<IActionResult> CreatePayout([FromRoute] string storeId, [FromForm] decimal amount, [FromForm] bool isOnChain)
        {
            try
            {
                var settings = await _pluginService.GetStoreSettings(storeId);
                if (settings == null)
                    return Json(new { success = false, error = "Store settings not found" });

                await _pluginService.CreatePayout(
                    settings.StoreId,
                    amount,
                    isOnChain);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

    }
}
