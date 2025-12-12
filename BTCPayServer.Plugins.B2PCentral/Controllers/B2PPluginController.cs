using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Data;
using BTCPayServer.Plugins.B2PCentral.Models;
using BTCPayServer.Plugins.B2PCentral.Models.P2P;
using BTCPayServer.Plugins.B2PCentral.Models.Swaps;
using BTCPayServer.Plugins.B2PCentral.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.B2PCentral;

[Route("~/plugins/{storeId}/b2pcentral")]
[AutoValidateAntiforgeryToken]

public class B2PPluginController(B2PCentralPluginService pluginService, UserManager<ApplicationUser> userManager) : Controller
{

    [HttpGet]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy= Policies.CanViewStoreSettings)]
    public async Task<IActionResult> Index(string storeId)
    {
        return View(await pluginService.GetStoreSettings(storeId));
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanModifyStoreSettings)]
    public async Task<IActionResult> Index(B2PSettings model, string command)
    {
        if (ModelState.IsValid)
        {
            switch (command)
            {
                case "Save":
                    await pluginService.UpdateSettings(model);
                    break;
                case "Test":
                    var sTest = await pluginService.TestB2P(model);
                    if (sTest == "OK")
                    {
                        TempData[WellKnownTempData.SuccessMessage] = "Access to B2P Central API successful";
                    }
                    else
                    {
                        TempData[WellKnownTempData.ErrorMessage] = $"Access to B2P Central API failed: {sTest}";
                    }
                    break;
            }
        }
        return View("Index", model);
    }


    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanViewStoreSettings)]
    [Route("GetPartialB2PResult")]
    public async Task<IActionResult> GetPartialB2PResult([FromBody] B2PRequest req)
    {
        var model = new B2PResult { Rate = req.Rate };
        try
        {
            var ofrReq = new OffersRequest
            {
                Amount = (uint)req.Amount,
                CurrencyCode = req.CurrencyCode,
                IsBuy = false,
                Providers = req.Providers
            };
            model.Offers = await pluginService.GetOffersListAsync(ofrReq, req.ApiKey);
        }
        catch (Exception ex)
        {
            model.ErrorMsg = ex.Message;
        }
        return PartialView("_B2PResults", model);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanViewStoreSettings)]
    [Route("GetPartialB2PSwapResult")]
    public async Task<IActionResult> GetPartialB2PSwapResult([FromBody] SwapRateRequestJS req)
    {
        var model = new B2PSwapResult();
        try
        {
            var user = await userManager.GetUserAsync(User);
            var swapReq = new SwapRateRequest
            {
                FromCrypto = "BTC",
                FromNetwork = "Bitcoin",
                ToCrypto = req.ToCrypto.Split("-")[0],
                ToNetwork = SwapCryptos.GetNetwork(req.ToCrypto),
                FromAmount = req.FromAmount,
                ToAmount = req.ToAmount,
                FiatCurrency = req.FiatCurrency,
                Providers = req.Providers
            };
            model.Swaps = await pluginService.GetSwapsListAsync(swapReq, req.ApiKey);
            model.ToCrypto = swapReq.ToCrypto;
            model.FiatCurrency = swapReq.FiatCurrency;
            model.RateRequest = swapReq;
            model.UserEmail = user?.Email ?? string.Empty;
        }
        catch (Exception ex)
        {
            model.ErrorMsg = ex.Message;
        }
        return PartialView("_B2PSwapResults", model);
    }
}
