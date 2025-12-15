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
        var model = new B2PViewModel
        {
            Settings = await pluginService.GetStoreSettings(storeId),
            Swaps = await pluginService.GetStoreSwaps(storeId)
        };
        return View(model);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanModifyStoreSettings)]
    public async Task<IActionResult> Index([FromRoute] string storeId, B2PSettings settings, string command)
    {
        if (ModelState.IsValid)
        {
            switch (command)
            {
                case "Save":
                    await pluginService.UpdateSettings(settings);
                    break;
                case "Test":
                    var sTest = await pluginService.TestB2P(settings);
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
        var model = new B2PViewModel
        {
            Settings = settings,
            Swaps = await pluginService.GetStoreSwaps(storeId)
        };
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
                FromAmount = Math.Round(req.FromAmount, 8, MidpointRounding.AwayFromZero),
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

    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanViewStoreSettings)]
    [Route("CreateSwap")]
    public async Task<IActionResult> CreateSwap([FromRoute] string storeId, [FromForm] SwapCreationRequestJS req)
    {
        try
        {
            var swap = new SwapCreationRequest
            {
                Provider = req.Provider,
                QuoteID = req.QuoteID ?? string.Empty,
                IsFixed = req.IsFixed,
                FromCrypto = "BTC",
                FromNetwork = "Bitcoin",
                ToCrypto = req.ToCrypto.Split("-")[0],
                ToNetwork = SwapCryptos.GetNetwork(req.ToCrypto),
                FromAmount = Math.Round(req.FromAmount, 8, MidpointRounding.AwayFromZero),
                ToAmount = req.ToAmount,
                NotificationEmail = req.NotificationEmail,
                ToAddress = req.ToAddress,
                FromRefundAddress = req.FromRefundAddress ?? string.Empty,
                NotificationNpub = string.Empty
            };
            var createdSwap =  await pluginService.CreateSwapAsync(storeId, swap, req.ApiKey);
            if (createdSwap != null && createdSwap.Success)
            {
                var sProvider = req.Provider.GetDisplayName();
                var t = await pluginService.CreatePayout(storeId, sProvider, createdSwap, req);
                var dbSwap = new B2PStoreSwap
                {
                    StoreId = storeId,
                    DateT = DateTime.UtcNow,
                    Provider = req.Provider,
                    SwapId = createdSwap.SwapId,
                    FollowUrl = createdSwap.FollowUrl,
                    FromAmount = req.FromAmount,
                    ToAmount = req.ToAmount,
                    ToCrypto = swap.ToCrypto,
                    ToNetwork = swap.ToNetwork,
                    BTCPayPullPaymentId = t.Item1,
                    BTCPayPayoutId = t.Item2
                };
                await pluginService.AddSwapInDb(dbSwap);
                TempData[WellKnownTempData.SuccessMessage] = $"Payout created! {sProvider} Offer ID: {createdSwap.SwapId}";
            }
            else
            {
                TempData[WellKnownTempData.ErrorMessage] = $"Error during swap creation";
            }

            return Json(createdSwap);
        }
        catch (Exception ex)
        {
            TempData[WellKnownTempData.ErrorMessage] = ex.Message;
        }
        return RedirectToAction("Index", routeValues: new { storeId = storeId });
    }
}
