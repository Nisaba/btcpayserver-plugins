using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Abstractions.Extensions;
using BTCPayServer.Client;
using BTCPayServer.Plugins.LnOnchainSwaps.Models;
using BTCPayServer.Plugins.LnOnchainSwaps.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BTCPayServer.PluginsLnOnchainSwaps.Controllers
{
    [Route("~/plugins/{storeId}/LnOnchainSwaps")]
    [Authorize(Policy = Policies.CanCreateNonApprovedPullPayments, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [Authorize(Policy = Policies.CanManagePayouts, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [AutoValidateAntiforgeryToken]
    public class LnOnchainSwapsPluginController(LnOnchainSwapsPluginService pluginService) : Controller
    {
        private readonly LnOnchainSwapsPluginService _pluginService = pluginService;

        [HttpGet]
        public async Task<IActionResult> Index([FromRoute] string storeId)
        {
            var model = new LnOnchainSwapsViewModel()
            {
                StoreId = storeId,
                Swaps = await _pluginService.GetStoreSwaps(storeId),
                IsPayoutCreated = (TempData[WellKnownTempData.SuccessMessage] ?? "").ToString().Contains("Payout created!")
            };
            return View(model);
        }

        [HttpPost]
        [Route("CreateSwap")]
        public async Task<IActionResult> CreateSwap([FromRoute] string storeId, [FromForm] SwapRequest reqClient)
        {
            try
            {
                BoltzSwap swap = null;
                var sRoot = Request.GetAbsoluteRoot();
                switch (reqClient.SwapType)
                {
                    case BoltzSwap.SwapTypeLnToOnChain:
                        var req = new LnToOnChainSwap
                        {
                            BtcAmount = reqClient.BtcAmount,
                            ToInternalOnChainWallet = reqClient.IsInternal,
                            ExternalOnChainAddress = reqClient.ExternalAddressOrInvoice
                        };
                        swap = await _pluginService.DoLnToOnchainSwap(storeId, sRoot, req);
                        break;
                    case BoltzSwap.SwapTypeOnChainToLn:
                        var req2 = new OnChainToLnSwap
                        {
                            BtcAmount = reqClient.BtcAmount,
                            ToInternalLnWalet = reqClient.IsInternal,
                            ExternalLnInvoice = reqClient.ExternalAddressOrInvoice
                        };
                        swap = await _pluginService.DoOnchainToLnSwap(storeId, sRoot, req2);
                        break;
                }
                TempData[WellKnownTempData.SuccessMessage] = $"Payout created! Boltz Swap ID: {swap.SwapId}";
            }
            catch (Exception ex)
            {
                TempData[WellKnownTempData.ErrorMessage] = ex.Message;
            }
            return RedirectToAction("Index", routeValues: new { storeId = storeId });
        }

        [HttpGet]
        [Route("GetSwapStatus")]
        public async Task<ActionResult> GetSwapStatus(string swapId)
        {
            try
            {
                var status = await _pluginService.DoGetSwapStatus(swapId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
