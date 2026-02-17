using BTCPayServer.Client;
using BTCPayServer.Client.Models;
using BTCPayServer.Data;
using BTCPayServer.Plugins.TelegramBot.Data;
using BTCPayServer.Plugins.TelegramBot.Models;
using BTCPayServer.Security.Greenfield;
using BTCPayServer.Services.Apps;
using BTCPayServer.Services.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.TelegramBot.Services
{
    public class TelegramBotPluginService(AppService appService,
                                          ILoggerFactory loggerFactory,
                                          TelegramBotDbContextFactory dbContextFactory,
                                          APIKeyRepository apiKeyRepository)
    {
        private readonly ILogger<TelegramBotPluginService> logger = loggerFactory.CreateLogger<TelegramBotPluginService>();
        public List<TelegramBot> telegramBots = new();

        private TelegramBotConfig? serverConfig = null;

        private TelegramBotAppData BuildAppData(AppData app, PointOfSaleSettings appSettings, string botToken, bool isEnabled)
        {
            return new TelegramBotAppData
            {
                Id = app.Id,
                Name = app.Name,
                StoreDataId = app.StoreDataId,
                CurrencyCode = appSettings.Currency,
                Title = appSettings.Title,
                BotToken = botToken,
                IsEnabled = isEnabled,
                ShopItems = AppService.Parse(appSettings.Template).ToList(),
                DefaultTaxRate = appSettings.DefaultTaxRate,
                FormId = appSettings.FormId
            };
        }

        public async Task InitConfig(string baseUrl, string userId)
        {
            try
            {
                var bUpdate = false;
                using (var context = dbContextFactory.CreateContext())
                {
                    var config = await context.Config.FirstOrDefaultAsync();
                    if (config == null)
                    {
                        var keyBytes = new byte[20];
                        RandomNumberGenerator.Fill(keyBytes);
                        var apiKey = Convert.ToHexString(keyBytes).ToLowerInvariant();

                        var apiKeyData = new APIKeyData
                        {
                            Id = apiKey,
                            Type = APIKeyType.Permanent,
                            Label = "Telegram Bot Plugin",
                            UserId = userId
                        };

                        apiKeyData.SetBlob(new APIKeyBlob
                        {
                            Permissions = new[] { Permission.Create(Policies.CanCreateInvoice).ToString() }
                        });

                        await apiKeyRepository.CreateKey(apiKeyData);

                        config = new TelegramBotConfig
                        { 
                            BaseUrl = baseUrl,
                            ApiKey = apiKey
                        };
                        context.Config.Add(config);
                        bUpdate = true;
                    }
                    else if (config.BaseUrl != baseUrl)
                    {
                        config.BaseUrl = baseUrl;
                        context.Config.Update(config);
                        bUpdate = true;
                    }
                    if (bUpdate)
                        await context.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "TelegramBotPlugin:InitBaseUrl()");
                throw;
            }
        }

        public TelegramBotConfig GetConfig()
        {
            if (serverConfig != null)
                return serverConfig;

            try
            {
                using (var context = dbContextFactory.CreateContext())
                {
                    serverConfig = context.Config.FirstOrDefault();
                    return serverConfig;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "TelegramBotPlugin:GetServerUrl()");
                throw;
            }
        }


        public async Task<TelegramBotViewModel> GetStoreViewModel(string storeId)
        {
            try
            {
                var storeApps = (await appService.GetApps("PointOfSale"))
                    .Where(a => !a.Archived && a.StoreDataId == storeId)
                    .ToList();

                var filteredApps = new List<TelegramBotAppData>();
                var invoices = new List<TelegramBotInvoices>();

                using (var context = dbContextFactory.CreateContext())
                {
                    var lstStoreSettings = await context.Settings
                        .Where(a => a.StoreId == storeId)
                        .ToListAsync();

                    foreach (var app in storeApps)
                    {
                        var appSettings = app.GetSettings<PointOfSaleSettings>();
                        if (appSettings.DefaultView != PointOfSale.PosViewType.Light)
                        {
                            var storeAppSettings = lstStoreSettings.FirstOrDefault(a => a.AppId == app.Id);
                            if (storeAppSettings == null)
                            {
                                storeAppSettings = new TelegramBotSettings
                                {
                                    StoreId = storeId,
                                    AppId = app.Id,
                                    BotToken = string.Empty,
                                    IsEnabled = false
                                };
                            }
                            filteredApps.Add(BuildAppData(app, appSettings, storeAppSettings.BotToken, storeAppSettings.IsEnabled));
                        }
                    }

                    invoices = await context.TelegramInvoices.Where(a => a.StoreId == storeId).ToListAsync();
                }

                return new TelegramBotViewModel()
                {
                    storeId = storeId,
                    StoreApps = filteredApps,
                    Invoices = invoices
                };
            }
            catch (Exception e)
            {
                logger.LogError(e, "TelegramBotPlugin:GetStoreViewModel()");
                throw;
            }
        }


        public async Task UpdateSettings(string storeId, string appId, string botToken, bool isEnabled)
        {
            try
            {
                bool bStart = false;
                bool bStop = false;
                var botService = telegramBots.FirstOrDefault(b => b.AppData.Id == appId);

                using (var context = dbContextFactory.CreateContext())
                {
                    var dbSettings = await context.Settings.FirstOrDefaultAsync(a => a.StoreId == storeId && a.AppId == appId);
                    if (dbSettings == null)
                    {
                        context.Settings.Add(new TelegramBotSettings
                        {
                            StoreId = storeId,
                            AppId = appId,
                            BotToken = botToken.Trim(),
                            IsEnabled = isEnabled
                        });

                        bStart = isEnabled;
                        if (botService == null)
                        {
                            var telegramBotLogger = loggerFactory.CreateLogger<TelegramBot>();
                            var app = (await appService.GetApps("PointOfSale"))
                                .FirstOrDefault(a => a.Id == appId && a.StoreDataId == storeId);
                            var appSettings = app.GetSettings<PointOfSaleSettings>();

                            var appData = BuildAppData(app, appSettings, botToken, isEnabled);
                            botService = new TelegramBot(appData, this, telegramBotLogger);
                            telegramBots.Add(botService);
                        }
                    }
                    else
                    {
                        if (dbSettings.IsEnabled != isEnabled)
                        {
                            if (isEnabled)
                                bStart = true;
                            else
                                bStop = true;
                        }

                        dbSettings.BotToken = botToken.Trim();
                        dbSettings.IsEnabled = isEnabled;
                        context.Settings.Update(dbSettings);
                    }
                    await context.SaveChangesAsync();
                }
                if (bStart && botService != null)
                {
                    _ = Task.Run(() => botService.StartBot(CancellationToken.None));
                }
                if (bStop && botService != null)
                {
                    botService.StopBot();
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "TelegramBotPlugin:UpdateSettings()");
                throw;
            }
        }

        public async Task LoadAndStartBots()
        {
            try
            {
                using (var context = dbContextFactory.CreateContext())
                {
                    var settingsList = await context.Settings.ToListAsync();
                    foreach (var settings in settingsList)
                    {
                        var app = (await appService.GetApps("PointOfSale"))
                            .FirstOrDefault(a => a.Id == settings.AppId && a.StoreDataId == settings.StoreId);
                        if (app != null)
                        {
                            var appSettings = app.GetSettings<PointOfSaleSettings>();
                            var appData = BuildAppData(app, appSettings, settings.BotToken, settings.IsEnabled);

                            var telegramBotLogger = loggerFactory.CreateLogger<TelegramBot>();
                            var botService = new TelegramBot(appData, this, telegramBotLogger);
                            telegramBots.Add(botService);

                            if (settings.IsEnabled)
                                _ = Task.Run(() => botService.StartBot(CancellationToken.None));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "TelegramBotPlugin:LoadAndStartBots()");
                //   throw;
            }
        }

        public async Task<string?> CreateInvoiceAsync(
            string storeId,
            string appId,
            string appName,
            decimal amount,
            string currency,
            List<PosCartItem> cartItems,
            string telegramUser,
            string? buyerEmail,
            string? buyerName,
            string? buyerAddress,
            string? buyerCity,
            string? buyerPostalCode,
            string? buyerCountry,
            long chatId)
        {
            try
            {
                var app = (await appService.GetApps("PointOfSale"))
                    .FirstOrDefault(a => a.Id == appId && a.StoreDataId == storeId);

                if (app == null)
                {
                    logger.LogError("App {AppId} non trouvée", appId);
                    return null;
                }

                var posSettings = app.GetSettings<PointOfSaleSettings>();

                var posData = new JObject
                {
                    ["cart"] = JArray.FromObject(cartItems.Select(item => new
                    {
                        id = item.Id,
                        title = item.Title,
                        count = item.Count,
                        price = item.Price
                    })),
                    ["subTotal"] = amount,
                    ["total"] = amount,
                    ["source"] = "telegram"
                };

                var invoiceReq = new CreateInvoiceRequest()
                {
                    Currency = currency,
                    Amount = amount,
                    Checkout = new InvoiceDataBase.CheckoutOptions()
                    {
                        DefaultLanguage = posSettings.HtmlLang
                    },
                    Metadata = JObject.FromObject(new
                    {
                        
                        itemDesc = $"From Telegram Bot: {posSettings.Title}" ,
                        appId = appId,
                        telegramUser = telegramUser,
                        chatId = chatId,
                        buyerEmail = buyerEmail,
                        buyerName = buyerName,
                        buyerAddress1 = buyerAddress,
                        buyerCity = buyerCity,
                        buyerZip = buyerPostalCode,
                        buyerCountry = buyerCountry,
                        posData = posData,
                    }),
                    Receipt = new InvoiceDataBase.ReceiptOptions() { Enabled = true }
                };

                var config = GetConfig();
                var btcpayUri = new Uri(config.BaseUrl);
                var client = new BTCPayServerClient(btcpayUri, config.ApiKey);
                var invoice = await client.CreateInvoice(storeId, invoiceReq);

                using (var context = dbContextFactory.CreateContext())
                {
                    var telegramInvoice = new TelegramBotInvoices
                    {
                        BTCPayInvoiceId = invoice.Id,
                        StoreId = storeId,
                        AppName = appName,
                        Amount = amount,
                        Currency = currency,
                        DateT = DateTime.UtcNow
                    };
                    context.TelegramInvoices.Add(telegramInvoice);
                    await context.SaveChangesAsync();
                }

                return invoice.CheckoutLink;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramBotPlugin:CreateInvoiceAsync()");
                return null;
            }
        }


        public async Task UpdateInventoryAsync(string appId, Dictionary<string, int> itemQuantities)
        {
            try
            {
                var changes = itemQuantities.Select(kv =>
                    new AppService.InventoryChange(kv.Key, -kv.Value)).ToArray();

                await appService.UpdateInventory(appId, changes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramBotPlugin:UpdateInventoryAsync()");
            }
        }

        public async Task<bool> RefreshBotAppData(string storeId, string appId)
        {
            try
            {
                var botService = telegramBots.FirstOrDefault(b => b.AppData.Id == appId);
                if (botService == null)
                    return false;

                var app = (await appService.GetApps("PointOfSale"))
                    .FirstOrDefault(a => a.Id == appId && a.StoreDataId == storeId);

                if (app == null)
                    return false;

                var appSettings = app.GetSettings<PointOfSaleSettings>();
                var updatedAppData = BuildAppData(app, appSettings, botService.AppData.BotToken, botService.AppData.IsEnabled);

                botService.UpdateAppData(updatedAppData);

                logger.LogInformation("TelegramBot AppData refreshed for app {AppId}", appId);
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e, "TelegramBotPlugin:RefreshBotAppData()");
                throw;
            }
        }
    }
}
