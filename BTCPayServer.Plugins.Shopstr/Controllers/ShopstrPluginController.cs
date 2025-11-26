using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Abstractions.Extensions;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Client;
using BTCPayServer.Plugins.Shopstr.Models;
using BTCPayServer.Plugins.Shopstr.Models.Shopstr;
using BTCPayServer.Plugins.Shopstr.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Shopstr.Controllers
{
    [Route("~/plugins/{storeId}/Shopstr")]
    [Authorize(Policy = Policies.CanModifyStoreSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [AutoValidateAntiforgeryToken]

    public class ShopstrPluginController (ShopstrPluginService pluginService, ShopstrService shopstrService) : Controller
    {
        private readonly ShopstrPluginService _pluginService = pluginService;
        private readonly ShopstrService _shopstrService = shopstrService;
        [HttpGet]
        public async Task<IActionResult> Index([FromRoute] string storeId)
        {
            var model = await _pluginService.GetStoreViewModel(storeId);
            return View(model);
        }

   /*     [HttpPost]
        [Route("SaveSettings")]
        public async Task<IActionResult> SaveSettings([FromRoute] string storeId, string shopstrShop)
        {
            try {
                await _pluginService.UpdateSettings(storeId, shopstrShop);
                TempData.SetStatusMessageModel(new StatusMessageModel()
                {
                    Message = "Shopstr settings updated",
                    Severity = StatusMessageModel.StatusSeverity.Success
                });
            }
            catch (System.Exception ex)
            {
                TempData.SetStatusMessageModel(new StatusMessageModel()
                {
                    Message = $"Error updating Shopstr settings: {ex.Message}",
                    Severity = StatusMessageModel.StatusSeverity.Error
                });
            }
            return RedirectToAction("Index", new { storeId = storeId });
        }*/

        [HttpPost]
        [Route("SendToShopstr")]
        public async Task SendToShopstr([FromRoute] string storeId, [FromForm] string appId)
        {
            var app = await _pluginService.GetStoreApp(appId);
            var nostrSettings = await _pluginService.GetNostrSettings(storeId);
          //  await _shopstrService.CreateShopstrProduct(app.ShopItems.First(), app.CurrencyCode, nostrSettings);
            var lst = await _shopstrService.GetShopstrProducts(nostrSettings.PubKey, nostrSettings.Relays);
        }

    }
}
