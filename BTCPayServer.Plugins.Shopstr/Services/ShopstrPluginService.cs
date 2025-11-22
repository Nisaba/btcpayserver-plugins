using BTCPayServer.Plugins.Shopstr.Data;
using BTCPayServer.Services.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using BTCPayServer.Plugins.Shopstr.Models;
using static Dapper.SqlMapper;
using System.Linq;
using BTCPayServer.Services.Apps;
using BTCPayServer.Data;

namespace BTCPayServer.Plugins.Shopstr.Services
{
    public class ShopstrPluginService
    {
        private readonly ILogger<ShopstrPluginService> _logger;
        private readonly ShopstrDbContextFactory _dbContextFactory;
        private readonly StoreRepository _storeRepository;
        private readonly AppService _appService;

        public ShopstrPluginService(
            ILogger<ShopstrPluginService> logger,
            ShopstrDbContextFactory contextFactory,
            StoreRepository storeRepository,
            AppService appService   )
        {
            _logger = logger;
            _dbContextFactory = contextFactory;
            _storeRepository = storeRepository;
            _appService = appService;
        }

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
                _logger.LogError(e, "ShopstrPlugin:GetStoreSettings()");
                throw;
            }
        }

    }
}
