using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Abstractions.Extensions;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Client;
using BTCPayServer.Plugins.Shopstr.Models;
using BTCPayServer.Plugins.Shopstr.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Shopstr.Controllers
{
    [Route("~/plugins/{storeId}/Shopstr")]
    [Authorize(Policy = Policies.CanModifyStoreSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [AutoValidateAntiforgeryToken]

    public class ShopstrPluginController (ShopstrPluginService pluginService, ShopstrService shopstrService) : Controller
    {

        [HttpGet]
        public async Task<IActionResult> Index([FromRoute] string storeId)
        {
            var model = await pluginService.GetStoreViewModel(storeId);
            return View(model);
        }

        [HttpPost]
        [Route("SaveSettings")]
        public async Task<IActionResult> SaveSettings([FromRoute] string storeId, [FromForm] string appId, [FromForm] string location, [FromForm] bool flashSales, [FromForm] string condition, [FromForm] int day, [FromForm] int month, [FromForm] int year, [FromForm] string restrictions)
        {
            var settings = new ShopstrSettings
            {
                StoreId = storeId,
                AppId = appId,
                Location = location.Trim(),
                FlashSales = flashSales,
                Condition = Enum.Parse<ConditionEnum>(condition),
                ValidDateT = day == 0 ? null : new DateTimeOffset(new DateTime(year, month, day, 0, 0, 0)),
                Restrictions = restrictions
            };
            await pluginService.UpdateSettings(settings);
            return Ok();
        }

        [HttpPost]
        [Route("PublishToShopstr")]
        public async Task<IActionResult> PublishToShopstr([FromRoute] string storeId, [FromForm] string appId)
        {
            try
            {
                var app = await pluginService.GetStoreApp(appId);
                if (!app.ShopItems.Any())
                {
                    return Ok();
                }
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                var nostrSettings = await pluginService.GetNostrSettings(storeId);

                await shopstrService.InitializeClient(nostrSettings.Relays);

                var productsFromShopstr = await shopstrService.GetShopstrProducts(nostrSettings.PubKey);

                foreach (var item in app.ShopItems)
                {
                    var existingProduct = productsFromShopstr.FirstOrDefault(p => p.Id == item.Id);
                    var bPublishToShopStr = existingProduct == null ? true : !existingProduct.Compare(item, app.Location);
                    if (bPublishToShopStr)
                    {
                        await shopstrService.CreateShopstrProduct(item, app.CurrencyCode, app.Location, nostrSettings, baseUrl);
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            finally
            {
                shopstrService.DisposeClient();
            }
        }

        [HttpPost]
        [Route("UnPublishFromShopstr")]
        public async Task<IActionResult> UnPublishFromShopstr([FromRoute] string storeId, [FromForm] string appId)
        {
            try
            {
                var app = await pluginService.GetStoreApp(appId);
                if (!app.ShopItems.Any())
                {
                    return Ok(); ;
                }

                var nostrSettings = await pluginService.GetNostrSettings(storeId);

                await shopstrService.InitializeClient(nostrSettings.Relays);
                var productsFromShopstr = await shopstrService.GetShopstrProducts(nostrSettings.PubKey);
                productsFromShopstr.RemoveAll(e => !e.Status);

                foreach (var item in app.ShopItems)
                {
                    if (productsFromShopstr.Any(p => p.Id == item.Id))
                        await shopstrService.CreateShopstrProduct(item, app.CurrencyCode, app.Location, nostrSettings, "", true);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            finally
            {
                shopstrService.DisposeClient();
            }
        }
    }
}
