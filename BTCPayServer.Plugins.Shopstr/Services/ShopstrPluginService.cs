using BTCPayServer.Plugins.PointOfSale;
using BTCPayServer.Plugins.Shopstr.Models.External;
using BTCPayServer.Plugins.Shopstr.Models.Shopstr;
using BTCPayServer.Services.Apps;
using BTCPayServer.Services.Stores;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Shopstr.Services
{
    public class ShopstrPluginService(ILogger<ShopstrPluginService> logger,
                                    StoreRepository storeRepository,
                                    AppService appService)
    {
        private readonly ILogger<ShopstrPluginService> _logger = logger;
        private readonly StoreRepository _storeRepository = storeRepository;
        private readonly AppService _appService = appService;

        public async Task<ShopstrViewModel> GetStoreViewModel(string storeId)
        {
            try
            {
                var storeApps = (await _appService.GetApps("PointOfSale"))
                    .Where(a => !a.Archived && a.StoreDataId == storeId)
                    .ToList();

                var filteredApps = new List<ShopstrAppData>();
                foreach (var app in storeApps)
                {
                    var appSettings = app.GetSettings<PointOfSaleSettings>();
                    if ( appSettings.DefaultView != PointOfSale.PosViewType.Light)
                    {
                        filteredApps.Add(new ShopstrAppData
                        {
                            Id = app.Id,
                            Name = app.Name,
                            StoreDataId = app.StoreDataId,
                            CurrencyCode = appSettings.Currency,
                            Location = ExtractGeoRegion(appSettings.HtmlMetaTags),
                            ShopItems = AppService.Parse(appSettings.Template).ToList()
                        });
                    }
                }

                return new ShopstrViewModel
                {
                    storeId = storeId,
                    Nip5Settings = await _storeRepository.GetSettingAsync<Nip5StoreSettings>(storeId, "NIP05"),
                    StoreApps = filteredApps
                };

            }
            catch (Exception e)
            {
                _logger.LogError(e, "ShopstrPlugin:GetStoreViewModel()");
                throw;
            }
        }

        private string ExtractGeoRegion(string htmlMetaTags)
        {
            if (string.IsNullOrEmpty(htmlMetaTags))
                return null;

            var pattern = @"<meta\s+name=[""']?geo\.region[""']?\s+content=[""']?([^""'>\s]+)[""']?";
            var match = Regex.Match(htmlMetaTags, pattern, RegexOptions.IgnoreCase);

            return match.Success ? match.Groups[1].Value : null;
        }

        /*    public async Task UpdateSettings(string storeId, string shopstrShop)
            {
                try
                {
                    using (var context = _dbContextFactory.CreateContext())
                    {
                        var dbSettings = await context.ShopstrSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                        if (dbSettings == null)
                        {
                            context.ShopstrSettings.Add( new ShopstrSettings
                            {
                                StoreId = storeId,
                                ShopStrShop = shopstrShop.Trim()
                            });
                        }
                        else
                        {
                            dbSettings.ShopStrShop = shopstrShop.Trim();
                            context.ShopstrSettings.Update(dbSettings);
                        }

                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "ShopstrPlugin:UpdateSettings()");
                    throw;
                }
            }*/

        public async Task<ShopstrAppData> GetStoreApp (string appId)
        {
            try {
                var app = await _appService.GetApp(appId, PointOfSaleAppType.AppType);
                if (app == null)
                    throw new Exception("App not found");
                var appSettings = app.GetSettings<PointOfSaleSettings>();
                return new ShopstrAppData
                {
                    Id = app.Id,
                    Name = app.Name,
                    StoreDataId = app.StoreDataId,
                    CurrencyCode = appSettings.Currency,
                    Location = ExtractGeoRegion(appSettings.HtmlMetaTags),
                    ShopItems = AppService.Parse(appSettings.Template).ToList()
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ShopstrPlugin:GetStoreApp()");
                throw;
            }
        }

        public async Task<Nip5StoreSettings> GetNostrSettings(string storeId)
        {
            try
            {
                return await _storeRepository.GetSettingAsync<Nip5StoreSettings>(storeId, "NIP05");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ShopstrPlugin:GetNostrSettings()");
                throw;
            }
        }
    }
}
