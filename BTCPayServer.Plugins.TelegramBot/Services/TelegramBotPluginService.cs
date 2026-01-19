using BTCPayServer.Plugins.TelegramBot.Models;
using BTCPayServer.Services.Apps;
using BTCPayServer.Services.Stores;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.TelegramBot.Services
{
    public class TelegramBotPluginService(AppService appService, ILogger<TelegramBotPluginService> logger)
    {
        public async Task<TelegramBotViewModel> GetStoreViewModel(string storeId)
        {
            try
            {
                var storeApps = (await appService.GetApps("PointOfSale"))
                    .Where(a => !a.Archived && a.StoreDataId == storeId)
                    .ToList();

                /*       var filteredApps = new List<ShopstrAppData>();
                       foreach (var app in storeApps)
                       {
                           var appSettings = app.GetSettings<PointOfSaleSettings>();
                           if (appSettings.DefaultView != PointOfSale.PosViewType.Light)
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
                       }*/

                return new TelegramBotViewModel();

            }
            catch (Exception e)
            {
                logger.LogError(e, "TelegramBotPlugin:GetStoreViewModel()");
                throw;
            }
        }

    }
}
