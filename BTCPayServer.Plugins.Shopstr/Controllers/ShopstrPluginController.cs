using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Plugins.Shopstr.Services;
using Microsoft.AspNetCore.Authorization;
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
        [Route("PublishToShopstr")]
        public async Task PublishToShopstr([FromRoute] string storeId, [FromForm] string appId)
        {
            var app = await _pluginService.GetStoreApp(appId);
            if (!app.ShopItems.Any())
            {
                return;
            }
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var nostrSettings = await _pluginService.GetNostrSettings(storeId);
            var productsFromShopstr = await _shopstrService.GetShopstrProducts(nostrSettings.PubKey, nostrSettings.Relays);

            foreach (var item in app.ShopItems)
            {
                var existingProduct = productsFromShopstr.FirstOrDefault(p => p.Id == item.Id);
                var bPublishToShopStr = existingProduct == null ? true : !existingProduct.Compare(item);
                if (bPublishToShopStr)
                {
                    await _shopstrService.CreateShopstrProduct(item, app.CurrencyCode, nostrSettings, baseUrl);
                }
            }
        }

        [HttpPost]
        [Route("UnPublishToShopstr")]
        public async Task UnPublishToShopstr([FromRoute] string storeId, [FromForm] string appId)
        {
            var app = await _pluginService.GetStoreApp(appId);
            if (!app.ShopItems.Any())
            {
                return;
            }
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var nostrSettings = await _pluginService.GetNostrSettings(storeId);

            foreach (var item in app.ShopItems)
            {
                await _shopstrService.CreateShopstrProduct(item, app.CurrencyCode, nostrSettings, baseUrl, true);
            }
        }

    }
}
