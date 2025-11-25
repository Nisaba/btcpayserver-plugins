using BTCPayServer.Plugins.Shopstr.Data;
using BTCPayServer.Services.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using static Dapper.SqlMapper;
using System.Linq;
using BTCPayServer.Services.Apps;
using BTCPayServer.Data;
using BTCPayServer.Plugins.Shopstr.Models.External;
using BTCPayServer.Plugins.Shopstr.Models.Shopstr;
using NBitcoin.Secp256k1;
using BTCPayServer.Plugins.PointOfSale;

namespace BTCPayServer.Plugins.Shopstr.Services
{
    public class ShopstrPluginService(ILogger<ShopstrPluginService> logger,
                                    ShopstrDbContextFactory contextFactory,
                                    StoreRepository storeRepository,
                                    AppService appService)
    {
        private readonly ILogger<ShopstrPluginService> _logger = logger;
        private readonly ShopstrDbContextFactory _dbContextFactory = contextFactory;
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
                            ShopItems = AppService.Parse(appSettings.Template).ToList()
                        });
                    }
                }

                using (var context = _dbContextFactory.CreateContext())
                {
                    var settings = await context.ShopstrSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                    if (settings == null)
                    {
                        settings = new ShopstrSettings
                        {
                            StoreId = storeId
                        };
                    }

                    return new ShopstrViewModel
                    {
                        storeId = storeId,
                        ShopstrSettings = settings,
                        SentItemsToShopstr = await context.ShopAppStoreItems.Where(a => a.StoreId == storeId).ToListAsync(),
                        Nip5Settings = await _storeRepository.GetSettingAsync<Nip5StoreSettings>(storeId, "NIP05"),
                        StoreApps = filteredApps
                    };
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, "ShopstrPlugin:GetStoreViewModel()");
                throw;
            }
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
