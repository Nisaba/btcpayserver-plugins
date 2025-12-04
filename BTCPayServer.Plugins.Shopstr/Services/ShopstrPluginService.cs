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
                                    ShopstrDbContextFactory shopstrDbContextFactory)
    {
        private readonly ILogger<ShopstrPluginService> _logger = logger;
        private readonly StoreRepository _storeRepository = storeRepository;
        private readonly AppService _appService = appService;
        private readonly ShopstrDbContextFactory _dbContextFactory = shopstrDbContextFactory;

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
                       var storeAppSettings = await GetSettings(storeId, app.Id);
                       filteredApps.Add(new ShopstrAppData
                        {
                            Id = app.Id,
                            Name = app.Name,
                            StoreDataId = app.StoreDataId,
                            CurrencyCode = appSettings.Currency,
                            Location = storeAppSettings?.Location ?? string.Empty,
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

        public async Task<ShopstrSettings> GetSettings(string storeId, string appId)
        {
            try
            {
                using (var context = _dbContextFactory.CreateContext())
                {
                    return await context.Settings.FirstOrDefaultAsync(a => a.StoreId == storeId && a.AppId == appId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ShopstrPlugin:GetSettings()");
                throw;
            }
        }
        public async Task UpdateSettings(string storeId, string appId, string location)
            {
                try
                {
                    using (var context = _dbContextFactory.CreateContext())
                    {
                        var dbSettings = await context.Settings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                        if (dbSettings == null)
                        {
                            context.Settings.Add( new ShopstrSettings
                            {
                                StoreId = storeId,
                                AppId = appId,
                                Location = location.Trim()
                            });
                        }
                        else
                        {
                            dbSettings.Location = location.Trim();
                            context.Settings.Update(dbSettings);
                        }

                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "ShopstrPlugin:UpdateSettings()");
                    throw;
                }
            }

        public async Task<ShopstrAppData> GetStoreApp (string appId)
        {
            try {
                var app = await _appService.GetApp(appId, PointOfSaleAppType.AppType);
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
