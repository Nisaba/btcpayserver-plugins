using BTCPayServer.Plugins.PointOfSale;
using BTCPayServer.Plugins.Shopstr.Data;
using BTCPayServer.Plugins.Shopstr.Models;
using BTCPayServer.Plugins.Shopstr.Models.External;
using BTCPayServer.Plugins.Shopstr.Models.Shopstr;
using BTCPayServer.Services.Apps;
using BTCPayServer.Services.Stores;
using Microsoft.EntityFrameworkCore;
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
                                    AppService appService,
                                    ShopstrDbContextFactory dbContextFactory)
    {
        public async Task<ShopstrViewModel> GetStoreViewModel(string storeId)
        {
            try
            {
                var storeApps = (await appService.GetApps("PointOfSale"))
                    .Where(a => !a.Archived && a.StoreDataId == storeId)
                    .ToList();

                var filteredApps = new List<ShopstrAppData>();
                foreach (var app in storeApps)
                {
                    var appSettings = app.GetSettings<PointOfSaleSettings>();
                    if ( appSettings.DefaultView != PointOfSale.PosViewType.Light)
                    {
                       var storeAppSettings = await GetSettings(storeId, app.Id);
                       filteredApps.Add(new ShopstrAppData
                        {
                            Id = app.Id,
                            Name = app.Name,
                            StoreDataId = app.StoreDataId,
                            CurrencyCode = appSettings.Currency,
                            Location = storeAppSettings?.Location ?? string.Empty,
                            FlashSales = storeAppSettings.FlashSales,
                            Condition = storeAppSettings.Condition,
                            ValidDateT = storeAppSettings.ValidDateT,
                            Restrictions = storeAppSettings.Restrictions,
                            ShopItems = AppService.Parse(appSettings.Template).ToList()
                        });
                    }
                }

                return new ShopstrViewModel
                {
                    storeId = storeId,
                    Nip5Settings = await storeRepository.GetSettingAsync<Nip5StoreSettings>(storeId, "NIP05"),
                    StoreApps = filteredApps
                };

            }
            catch (Exception e)
            {
                logger.LogError(e, "ShopstrPlugin:GetStoreViewModel()");
                throw;
            }
        }

        public async Task<ShopstrSettings> GetSettings(string storeId, string appId)
        {
            try
            {
                using (var context = dbContextFactory.CreateContext())
                {
                    return await context.Settings.FirstOrDefaultAsync(a => a.StoreId == storeId && a.AppId == appId);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "ShopstrPlugin:GetSettings()");
                throw;
            }
        }

        public async Task UpdateSettings(ShopstrSettings settings)
            {
                try
                {
                    using (var context = dbContextFactory.CreateContext())
                    {
                        var dbSettings = await context.Settings.FirstOrDefaultAsync(a => a.StoreId == settings.StoreId);
                        if (dbSettings == null)
                        {
                            context.Settings.Add( settings);
                        }
                        else
                        {
                            dbSettings.Location = settings.Location;
                            dbSettings.FlashSales = settings.FlashSales;
                            dbSettings.Condition = settings.Condition;
                            dbSettings.ValidDateT = settings.ValidDateT;
                            dbSettings.Restrictions = settings.Restrictions;
                            context.Settings.Update(dbSettings);
                        }

                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "ShopstrPlugin:UpdateSettings()");
                    throw;
                }
            }

        public async Task<ShopstrAppData> GetStoreApp (string appId)
        {
            try {
                var app = await appService.GetApp(appId, PointOfSaleAppType.AppType);
                if (app == null)
                    throw new Exception("App not found");
                var appSettings = app.GetSettings<PointOfSaleSettings>();
                var shopstrSettings = await GetSettings(app.StoreDataId, app.Id);
                return new ShopstrAppData
                {
                    Id = app.Id,
                    Name = app.Name,
                    StoreDataId = app.StoreDataId,
                    CurrencyCode = appSettings.Currency,
                    Location = shopstrSettings?.Location ?? string.Empty,
                    ShopItems = AppService.Parse(appSettings.Template).ToList()
                };
            }
            catch (Exception e)
            {
                logger.LogError(e, "ShopstrPlugin:GetStoreApp()");
                throw;
            }
        }

        public async Task<Nip5StoreSettings> GetNostrSettings(string storeId)
        {
            try
            {
                return await storeRepository.GetSettingAsync<Nip5StoreSettings>(storeId, "NIP05");
            }
            catch (Exception e)
            {
                logger.LogError(e, "ShopstrPlugin:GetNostrSettings()");
                throw;
            }
        }
    }
}
