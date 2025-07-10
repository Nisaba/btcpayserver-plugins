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

        [HttpGet, HttpPost]
        [Authorize(Policy = Policies.CanModifyStoreSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        [Authorize(Policy = Policies.CanCreateNonApprovedPullPayments, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        [Authorize(Policy = Policies.CanManagePayouts, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> Index([FromRoute] string storeId, [FromForm] string? peachMsg, [FromForm] string? peachToken)
        {
            var model = new PeachViewModel()
            {
                Settings = await _pluginService.GetStoreSettings(storeId),
                IsPayoutCreated = false
            };
            if (!string.IsNullOrEmpty(peachMsg))
            {
                if (peachMsg.Contains("Error", StringComparison.OrdinalIgnoreCase))
                {
                    TempData[WellKnownTempData.ErrorMessage] = peachMsg;
                }
                else
                {
                    TempData[WellKnownTempData.SuccessMessage] = peachMsg;
                }
            }
            if (!string.IsNullOrEmpty(peachToken))
            {
                model.PeachToken = peachToken;
            }
            else
            {
                model.PeachToken = model.Settings.IsRegistered ? await _peachService.GetToken(model.Settings) : string.Empty;
            }

            return View(model);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanModifyStoreSettings)]
        [Route("UpdSettings")]
        public async Task<IActionResult> UpdSettings([FromBody] PeachSettings settings)
        {
            string sMsg = "";
            string sToken = "";
            if (!ModelState.IsValid)
            {
                sMsg = "Error in data";
            } 
            else 
            { 
                try
                {
                    sToken = await _peachService.GetToken(settings);
                    sMsg = "Peach token received successfully... ";
                    settings.IsRegistered = true;

                    await _pluginService.UpdateSettings(settings);
                    sMsg += "Settings successfuly saved";
                }
                catch (Exception ex)
                {
                    sMsg += $"Error: {ex.Message}";
                }
            }
            return Json(new { msg = sMsg, token = sToken });
        }


        [HttpPost]
        [Authorize(Policy = Policies.CanCreateNonApprovedPullPayments, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        [Authorize(Policy = Policies.CanManagePayouts, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        [Route("GetPartialResult")]
        public async Task<IActionResult> GetPartialResult([FromBody] PeachRequest req)
        {
            var model = new PeachResult { CurrencyCode = req.CurrencyCode };
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
