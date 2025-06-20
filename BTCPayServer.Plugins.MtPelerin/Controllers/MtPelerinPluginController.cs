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
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanViewInvoices)]
    public class MtPelerinPluginController(MtPelerinPluginService pluginService) : Controller
    {
        private readonly MtPelerinPluginService _pluginService = pluginService;

        [HttpGet]
        public async Task<IActionResult> Index([FromRoute] string storeId)
        {
            var model = new MtPelerinModel
            {
                Settings = await _pluginService.GetStoreSettings(storeId),
                Transactions = await _pluginService.GetStoreTransactions(storeId)
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(MtPelerinModel model, string command)
        {
            if (!ModelState.IsValid)
            {
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
                    throw;
                }
           }
            return RedirectToAction("Index");
        }
    }
}
