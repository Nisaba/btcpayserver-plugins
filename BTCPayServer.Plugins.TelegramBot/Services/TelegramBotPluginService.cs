using BTCPayServer.Plugins.TelegramBot.Data;
using BTCPayServer.Plugins.TelegramBot.Models;
using BTCPayServer.Services.Apps;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.TelegramBot.Services
{
    public class TelegramBotPluginService(AppService appService,
                                          ILogger<TelegramBotPluginService> logger,
                                          TelegramBotDbContextFactory dbContextFactory)
    {
        public async Task<TelegramBotViewModel> GetStoreViewModel(string storeId)
        {
            try
            {
                var storeApps = (await appService.GetApps("PointOfSale"))
                    .Where(a => !a.Archived && a.StoreDataId == storeId)
                    .ToList();

                       var filteredApps = new List<TelegramBotAppData>();
                       foreach (var app in storeApps)
                       {
                           var appSettings = app.GetSettings<PointOfSaleSettings>();
                           if (appSettings.DefaultView != PointOfSale.PosViewType.Light)
                           {
                               var storeAppSettings = await GetSettings(storeId, app.Id);
                               filteredApps.Add(new TelegramBotAppData
                               {
                                   Id = app.Id,
                                   Name = app.Name,
                                   StoreDataId = app.StoreDataId,
                                   CurrencyCode = appSettings.Currency,
                                   Title = appSettings.Title,
                                   BotToken = storeAppSettings.BotToken,
                                   IsEnabled = storeAppSettings.IsEnabled,
                                   ShopItems = AppService.Parse(appSettings.Template).ToList()
                               });
                           }
                       }

                return new TelegramBotViewModel()
                {
                    storeId = storeId,
                    StoreApps = filteredApps
                };

            }
            catch (Exception e)
            {
                logger.LogError(e, "TelegramBotPlugin:GetStoreViewModel()");
                throw;
            }
        }

        public async Task<TelegramBotSettings> GetSettings(string storeId, string appId)
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
                logger.LogError(e, "TelegramBotPlugin:GetSettings()");
                throw;
            }
        }

        public async Task UpdateSettings(string storeId, string appId, string botToken, bool isEnabled)
        {
            try
            {
                using (var context = dbContextFactory.CreateContext())
                {
                    var dbSettings = await context.Settings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                    if (dbSettings == null)
                    {
                        context.Settings.Add(new TelegramBotSettings
                        {
                            StoreId = storeId,
                            AppId = appId,
                            BotToken = botToken.Trim(),
                            IsEnabled = isEnabled
                        });
                    }
                    else
                    {
                        dbSettings.BotToken = botToken.Trim();
                        dbSettings.IsEnabled = isEnabled;
                        context.Settings.Update(dbSettings);
                    }

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "TelegramBotPlugin:UpdateSettings()");
                throw;
            }
        }



    }
}
