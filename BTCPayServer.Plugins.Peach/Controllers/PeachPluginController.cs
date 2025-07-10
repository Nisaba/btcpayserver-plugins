using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Plugins.Peach.Model;
using BTCPayServer.Plugins.Peach.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Peach.Controllers
{
    [Route("~/plugins/{storeId}/Peach")]


    public class PeachPluginController(PeachPluginService pluginService, PeachService peachService) : Controller
    {
        private readonly PeachPluginService _pluginService = pluginService;
        private readonly PeachService _peachService = peachService;

        [HttpGet]
        [Authorize(Policy = Policies.CanModifyStoreSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        [Authorize(Policy = Policies.CanCreateNonApprovedPullPayments, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        [Authorize(Policy = Policies.CanManagePayouts, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> Index([FromRoute] string storeId)
        {
            var model = new PeachViewModel()
            {
                Settings = await _pluginService.GetStoreSettings(storeId),
                IsPayoutCreated = (TempData[WellKnownTempData.SuccessMessage] ?? "").ToString().Contains("Payout created!")
            };
           
            return View(model);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanModifyStoreSettings)]
        public async Task<IActionResult> Index(PeachViewModel model, string command)
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


        [HttpPost]
        [Authorize(Policy = Policies.CanCreateNonApprovedPullPayments, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        [Authorize(Policy = Policies.CanManagePayouts, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        [Route("GetPartialResult")]
        public async Task<IActionResult> GetPartialResult([FromBody] PeachRequest req)
        {
            var model = new PeachResult();
            try
            {

                model.Bids = await _peachService.GetBidsListAsync(req);

            }
            catch (Exception ex)
            {
                model.ErrorMsg = ex.Message;
            }
            return PartialView("_PeachResults", model);
        }
    }
}
