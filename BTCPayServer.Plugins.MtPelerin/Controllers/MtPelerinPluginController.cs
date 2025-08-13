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
    [AutoValidateAntiforgeryToken]
    public class MtPelerinPluginController(MtPelerinPluginService pluginService) : Controller
    {
        private readonly MtPelerinPluginService _pluginService = pluginService;

        [HttpGet]
        public async Task<IActionResult> Index([FromRoute] string storeId)
        {
            var model = new MtPelerinModel()
            {
                Settings = await _pluginService.GetStoreSettings(storeId),
                IsPayoutCreated = (TempData[WellKnownTempData.SuccessMessage] ?? "").ToString().Contains("Payout created!")
            };
           
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(MtPelerinModel model, string command)
        {
            if (!ModelState.IsValid)
            {
                TempData[WellKnownTempData.ErrorMessage] = $"Error in data";
                return View(model);
            }
            if (command == "save")
            {
                try
                {
                    await _pluginService.UpdateSettings(model.Settings);
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
        public async Task<IActionResult> CreatePayout([FromRoute] string storeId, [FromBody] MtPelerinOperation operation)
        {
            try
            {
                var settings = await _pluginService.GetStoreSettings(storeId);
                if (settings == null)
                {
                    TempData[WellKnownTempData.ErrorMessage] = "Store settings not found";
                    return RedirectToAction("Index");
                }

                await _pluginService.CreatePayout(
                    settings.StoreId,
                    operation);

                TempData[WellKnownTempData.SuccessMessage] = $"Payout created! Mt Pelerin ID: {operation.MtPelerinId}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData[WellKnownTempData.ErrorMessage] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

    }
}
