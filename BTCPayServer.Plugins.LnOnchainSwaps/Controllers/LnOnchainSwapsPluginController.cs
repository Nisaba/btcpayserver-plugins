﻿using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Abstractions.Extensions;
using BTCPayServer.Client;
using BTCPayServer.Plugins.LnOnchainSwaps.Models;
using BTCPayServer.Plugins.LnOnchainSwaps.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities.Collections;
using System;
using System.Text;
using System.Text.Json;
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
            var bHasPrivateKey = await _pluginService.InitSettings(storeId);
            var model = new LnOnchainSwapsViewModel()
            {
                StoreId = storeId,
                HasPrivateKey = bHasPrivateKey,
                WalletConfig = await _pluginService.GetBalances(storeId, $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}"),
                Swaps = await _pluginService.GetStoreSwaps(storeId),
                IsPayoutCreated = (TempData[WellKnownTempData.SuccessMessage] ?? "").ToString().Contains("Payout created!")
            };
            if (!model.WalletConfig.OffChainAvailable || !model.WalletConfig.OnChainAvailable)
            {
                TempData[WellKnownTempData.ErrorMessage] = "Onchain and Lightning wallets mut be configured for this store";
            }
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
                            ExternalOnChainAddress = reqClient.ExternalAddressOrInvoice,
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

        [HttpGet("DownloadRefundJson")]
        public async Task<FileContentResult> DownloadRefundJson([FromRoute] string storeId)
        {
            var settings = await _pluginService.GetStoreSettings(storeId);
            if (settings == null || !settings.HasPrivateKey)
            {
                throw new InvalidOperationException("No private key for this store");
            }
            var data = new { mnemonic = settings.RefundMnemonic }; 
            string jsonString = System.Text.Json.JsonSerializer.Serialize(data);
            byte[] fileBytes = Encoding.UTF8.GetBytes(jsonString);
            return new FileContentResult(fileBytes, "application/json")
            {
                FileDownloadName = $"boltz-rescue-key-btcpay-{storeId}.json"
            };
        }

        [HttpPost]
        [Route("GetInfosFromHW")]
        public async Task<IActionResult> GetInfosFromHW([FromRoute] string storeId, [FromForm] string vaultResponse)
        {
            try
            {
                dynamic vaultData = JsonConvert.DeserializeObject<dynamic>(vaultResponse);
                if (!vaultData.Success)
                    return BadRequest($"Vault signing failed: {vaultData.Error}");

                await _pluginService.InitSettingsFromHW(storeId, vaultData);
                return RedirectToAction("Index", routeValues: new { storeId = storeId });
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to generate rescue file");
            }
        }

    }
}
