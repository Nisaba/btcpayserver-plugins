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

namespace BTCPayServer.Plugins.Shopstr.Services
{
    public class ShopstrPluginService
    {
        private readonly ILogger<ShopstrPluginService> _logger;
        private readonly ShopstrDbContextFactory _dbContextFactory;
        private readonly StoreRepository _storeRepository;

        public ShopstrPluginService(
            ILogger<ShopstrPluginService> logger,
            ShopstrDbContextFactory contextFactory,
            StoreRepository storeRepository)
        {
            _logger = logger;
            _dbContextFactory = contextFactory;
            _storeRepository = storeRepository;
        }

        public async Task<ShopstrViewModel> GetStoreViewModel(string storeId)
        {
            try
            {
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

                    var items = await context.ShopAppStoreItems.Where(a => a.StoreId == storeId).ToListAsync();

                    return new ShopstrViewModel
                    {
                        ShopstrSettings = settings,
                        ShopAppStoreItems = items,
                        Nip5Settings = await _storeRepository.GetSettingAsync<Nip5StoreSettings>(storeId, "NIP05")
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
