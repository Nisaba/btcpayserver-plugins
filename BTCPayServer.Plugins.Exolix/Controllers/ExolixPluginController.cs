using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Plugins.Exolix.Model;
using BTCPayServer.Plugins.Exolix.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Exolix.Controllers
{
    [Route("~/plugins/{storeId}/Exolix")]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanModifyStoreSettings)]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanViewInvoices)]
    [Authorize(Policy = Policies.CanCreateNonApprovedPullPayments, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [Authorize(Policy = Policies.CanManagePayouts, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [AutoValidateAntiforgeryToken]
    public class ExolixPluginController(ExolixPluginService pluginService, ExolixService exolixService) : Controller
    {

        [HttpGet]
        public async Task<IActionResult> Index([FromRoute] string storeId)
        {
            var model = new ExolixModel
            {
                Settings = await pluginService.GetStoreSettings(storeId),
                Transactions = await pluginService.GetStoreTransactions(storeId),
                MerchantTransactions = await pluginService.GetStoreMerchantTransactions(storeId)
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(ExolixSettings settings, string command)
        {
            if (ModelState.IsValid && command == "save")
            {
                try
                {
                    settings.AcceptedCryptos ??= new List<string>();
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

        [HttpPost]
        [Route("SwapMerchant")]
        public async Task<IActionResult> SwapMerchant([FromRoute] string storeId, [FromForm] SwapMerchantRequest req)
        {
            var rep = new SwapCreationResponse();
            try
            {
                string sToCrypto, sToNetwork;
                if (req.ToCrypto.Contains("-"))
                {
                    var sSplit = req.ToCrypto.Split('-');
                    sToCrypto = sSplit[0];
                    sToNetwork = sSplit[1];
                }
                else
                {
                    sToCrypto = req.ToCrypto;
                    sToNetwork = req.ToCrypto;
                }
                var exolixSwapReq = new SwapCreationRequest
                {
                    FromCrypto = "BTC",
                    FromNetwork = "BTC",
                    FromAmount = req.BtcAmount,
                    ToCrypto = sToCrypto,
                    ToNetwork = sToNetwork,
                    ToAmount = req.ToAmount,
                    ToAddress = req.ToAddress,
                };
                rep = await exolixService.CreateSwapAsync(exolixSwapReq);
//              rep = new SwapCreationResponse { SwapId = "test-swap-id", FromAmount = req.BtcAmount, StatusMessage = "Swap created successfully" };
#if DEBUG
                rep.FromAddress = "bcrt1qpzfyktpawhcy66ctqpujdhfxsm8atjqzezq9p4";
#endif

                var sPPId = await pluginService.CreatePayout(storeId, rep.SwapId, rep.FromAddress, (decimal)rep.FromAmount);

                await pluginService.AddStoreMerchantTransaction(new ExolixMerchantTx
                {
                    StoreId = storeId,
                    AltcoinTo = req.ToCrypto,
                    DateT = DateTime.UtcNow,
                    BTCAmount = rep.FromAmount,
                    AltAmount = req.ToAmount,
                    TxID = rep.SwapId,
                    BTCPayPullPaymentId = sPPId,
                });

                TempData[WellKnownTempData.SuccessMessage] = "Exolix Swap successfully created: " + rep.SwapId;
                TempData["SwapId"] = rep.SwapId;
            }
            catch (Exception ex)
            {
                TempData[WellKnownTempData.ErrorMessage] = "Error during Swap creation: " + ex.Message;
            }
            return RedirectToAction("Index", new { storeId = RouteData.Values["storeId"] });
        }
    }
}
