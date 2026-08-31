using BTCPayServer.Abstractions.Constants;
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

    public class ShopstrPluginController (ShopstrPluginService pluginService, ShopstrService shopstrService, WooCommerceService wooCommerceService) : Controller
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
                    var bPublishToShopStr = existingProduct == null ? true : !existingProduct.Compare(item, app);
                    if (bPublishToShopStr)
                    {
                        await shopstrService.CreateShopstrProduct(item, app, nostrSettings, baseUrl);
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
                    return Ok();
                }

                var nostrSettings = await pluginService.GetNostrSettings(storeId);

                await shopstrService.InitializeClient(nostrSettings.Relays);
                var productsFromShopstr = await shopstrService.GetShopstrProducts(nostrSettings.PubKey);
                productsFromShopstr.RemoveAll(e => !e.Status);

                foreach (var item in app.ShopItems)
                {
                    if (productsFromShopstr.Any(p => p.Id == item.Id))
                        await shopstrService.CreateShopstrProduct(item, app, nostrSettings, "", true);
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
        [Route("SaveWooCommerceSettings")]
        public async Task<IActionResult> SaveWooCommerceSettings([FromRoute] string storeId,
            [FromForm] string wooCommerceUrl, [FromForm] string consumerKey, [FromForm] string consumerSecret,
            [FromForm] string location, [FromForm] bool flashSales, [FromForm] string condition, [FromForm] string restrictions)
        {
            var settings = new WooCommerceSettings
            {
                StoreId = storeId,
                WooCommerceUrl = wooCommerceUrl?.Trim(),
                ConsumerKey = consumerKey?.Trim(),
                ConsumerSecret = consumerSecret?.Trim(),
                Location = location?.Trim() ?? "",
                FlashSales = flashSales,
                Condition = Enum.Parse<ConditionEnum>(condition ?? "None"),
                Restrictions = restrictions ?? ""
            };
            await pluginService.UpdateWooCommerceSettings(settings);
            return Ok();
        }

        [HttpPost]
        [Route("PublishWooCommerce")]
        public async Task<IActionResult> PublishWooCommerce([FromRoute] string storeId)
        {
            try
            {
                var wcSettings = await pluginService.GetWooCommerceSettings(storeId);
                if (wcSettings == null || !wcSettings.IsConfigured)
                    return BadRequest("WooCommerce is not configured");

                var nostrSettings = await pluginService.GetNostrSettings(storeId);
                if (nostrSettings == null || !nostrSettings.IsConfigured)
                    return BadRequest("Nostr plugin is not configured");

                var wcApp = await wooCommerceService.FetchProducts(wcSettings);
                if (!wcApp.ShopItems.Any())
                    return Ok();

                var baseUrl = wcSettings.WooCommerceUrl.TrimEnd('/');
                await shopstrService.InitializeClient(nostrSettings.Relays);

                var productsFromShopstr = await shopstrService.GetShopstrProducts(nostrSettings.PubKey);

                foreach (var item in wcApp.ShopItems)
                {
                    var existingProduct = productsFromShopstr.FirstOrDefault(p => p.Id == item.Id);
                    var bPublish = existingProduct == null || !existingProduct.Compare(item, wcApp);
                    if (bPublish)
                    {
                        await shopstrService.CreateShopstrProduct(item, wcApp, nostrSettings, baseUrl);
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
        [Route("UnPublishWooCommerce")]
        public async Task<IActionResult> UnPublishWooCommerce([FromRoute] string storeId)
        {
            try
            {
                var wcSettings = await pluginService.GetWooCommerceSettings(storeId);
                if (wcSettings == null || !wcSettings.IsConfigured)
                    return BadRequest("WooCommerce is not configured");

                var nostrSettings = await pluginService.GetNostrSettings(storeId);
                var wcApp = await wooCommerceService.FetchProducts(wcSettings);
                if (!wcApp.ShopItems.Any())
                    return Ok();

                await shopstrService.InitializeClient(nostrSettings.Relays);
                var productsFromShopstr = await shopstrService.GetShopstrProducts(nostrSettings.PubKey);
                productsFromShopstr.RemoveAll(e => !e.Status);

                foreach (var item in wcApp.ShopItems)
                {
                    if (productsFromShopstr.Any(p => p.Id == item.Id))
                        await shopstrService.CreateShopstrProduct(item, wcApp, nostrSettings, "", true);
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
