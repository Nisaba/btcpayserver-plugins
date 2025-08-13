using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Plugins.Lendasat.Models;
using BTCPayServer.Plugins.Lendasat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Lendasat.Controllers
{
    [Route("~/plugins/{storeId}/Lendasat")]
    [AutoValidateAntiforgeryToken]


    public class LendasatPluginController(LendasatPluginService pluginService, LendasatService lendasatService) : Controller
    {
        private readonly LendasatPluginService _pluginService = pluginService;
        private readonly LendasatService _lendasatService = lendasatService;

        [HttpGet]
        [Authorize(Policy = Policies.CanModifyStoreSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        [Authorize(Policy = Policies.CanCreateNonApprovedPullPayments, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        [Authorize(Policy = Policies.CanManagePayouts, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> Index(string storeId)
        {
            var model = new LendasatViewModel
            {
                Settings = await _pluginService.GetStoreSettings(storeId)
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanModifyStoreSettings)]
        public async Task<IActionResult> Index(LendasatSettings settings)
        {
            if (ModelState.IsValid)
            {
                 await _pluginService.UpdateSettings(settings);
            }
            return View("Index", new LendasatViewModel { Settings = settings} );
        }
    }
}
