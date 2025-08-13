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
    [AutoValidateAntiforgeryToken]

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
                MeansOfPayments = await _pluginService.GetMoPNames(storeId),
                IsPayoutCreated = (TempData[WellKnownTempData.SuccessMessage] ?? "").ToString().Contains("Payout created!")
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
        public async Task<IActionResult> UpdSettings([FromBody] UpdateRequest req)
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
                    sToken = await _peachService.GetToken(req.Settings);
                    sMsg = "Peach token received, ";
                    req.Settings.IsRegistered = true;

                    await _pluginService.UpdateSettings(req.Settings);
                    sMsg += "Settings saved, ";

                    await _pluginService.UpdateMeansOfPayments(req.Settings.StoreId, req.MeansOfPayments);
                    sMsg += "Means of Payments saved... Success";
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
        [Route("GetToken")]
        public async Task<IActionResult> GetToken([FromRoute] string storeId, [FromBody] dynamic req)
        {
            string sMsg = "";
            string sToken = "";
            try
            {
                var settings = await _pluginService.GetStoreSettings(storeId);
                settings.Pwd = req["pwd"];
                sToken = await _peachService.GetToken(settings);
                sMsg = "Peach token received";
            }
            catch (Exception ex)
            {
                sMsg += $"Error: {ex.Message}";
            }
            return Json(new { msg = sMsg, token = sToken });
        }

        [HttpPost]
        [Authorize(Policy = Policies.CanCreateNonApprovedPullPayments, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        [Authorize(Policy = Policies.CanManagePayouts, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        [Route("GetPartialResult")]
        public async Task<IActionResult> GetPartialResult([FromRoute] string storeId, [FromBody] PeachRequest req)
        {
            var model = new PeachResult { CurrencyCode = req.CurrencyCode };
            try
            {
                var mopNames = await _pluginService.GetMoPNames(storeId);
                model.Bids = await _peachService.GetBidsListAsync(req, mopNames);

            }
            catch (Exception ex)
            {
                model.ErrorMsg = ex.Message;
            }
            return PartialView("_PeachResults", model);
        }

        [HttpPost]
        [Authorize(Policy = Policies.CanCreateNonApprovedPullPayments, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        [Authorize(Policy = Policies.CanManagePayouts, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        [Route("CreateOffer")]
        public async Task<IActionResult> CreateOffer([FromRoute] string storeId, [FromForm] PeachClientPostOfferRequest reqCient)
        {
            try
            {
                var req = new PeachPostOfferRequest
                {
                    PeachToken = reqCient.Token,
                    Amount = reqCient.Amount,
                    Premium = reqCient.Premium,
                    CurrencyCode = reqCient.Currency,
                    MeansOfPayment = await _pluginService.GetMeansOfPayments(storeId),
                    ReturnAdress = await _pluginService.GetWalletBtcAddress(storeId)
                };
                var offerId = await _peachService.PostSellOffer(req);

                var settings = await _pluginService.GetStoreSettings(storeId);
                var btcEscrowAddress = await _peachService.CreateEscrow(reqCient.Token, offerId, settings.PrivKey) ;

                await _pluginService.CreatePayout(storeId, offerId, btcEscrowAddress, reqCient.Amount);
                TempData[WellKnownTempData.SuccessMessage] = $"Payout created! Peach Offer ID: {offerId}";
            }
            catch (Exception ex)
            {
                TempData[WellKnownTempData.ErrorMessage] = ex.Message;
            }
            return RedirectToAction("Index", routeValues: new { storeId = storeId });
        }
    }
}
