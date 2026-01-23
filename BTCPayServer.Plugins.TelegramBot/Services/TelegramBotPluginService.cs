using BTCPayServer.Data;
using BTCPayServer.Plugins.TelegramBot.Data;
using BTCPayServer.Plugins.TelegramBot.Models;
using BTCPayServer.Services.Apps;
using BTCPayServer.Services.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.TelegramBot.Services
{
    public class TelegramBotPluginService(AppService appService,
                                          ILoggerFactory loggerFactory,
                                          TelegramBotDbContextFactory dbContextFactory)
    {
        private readonly ILogger<TelegramBotPluginService> logger = loggerFactory.CreateLogger<TelegramBotPluginService>();
        private List<TelegramBot> telegramBots = new();

        private string? serverBaseUrl = null;

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
            };
        }

        public async Task InitBaseUrl(string baseUrl)
        {
            try
            {
                var bUpdate = false;
                using (var context = dbContextFactory.CreateContext())
                {
                    var config = await context.Config.FirstOrDefaultAsync();
                    if (config == null)
                    {
                        context.Config.Add(baseUrl);
                        bUpdate = true;
                    }
                    else if (config != baseUrl)
                    {
                        context.Config.Update(baseUrl);
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

        public string GetServerUrl()
        {
            if (serverBaseUrl != null)
                return serverBaseUrl;

            try
            {
                using (var context = dbContextFactory.CreateContext())
                {
                    var config = context.Config.FirstOrDefault();
                    if (config != null)
                    {
                        serverBaseUrl = config;
                        return config;
                    }
                    return string.Empty;
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
            string? buyerEmail,
            string? buyerName,
            string? buyerAddress,
            string? buyerCity,
            string? buyerPostalCode,
            string? buyerCountry,
            string orderId)
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

                // Construire les données POS pour le panier
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

                // Créer les métadonnées de la facture
                var metadata = new InvoiceMetadata
                {
                    OrderId = orderId,
                    BuyerEmail = buyerEmail,
                    BuyerName = buyerName,
                    BuyerAddress1 = buyerAddress,
                    BuyerCity = buyerCity,
                    BuyerZip = buyerPostalCode,
                    BuyerCountry = buyerCountry,
                    PosData = posData,
                    ItemDesc = string.Join(", ", cartItems.Select(c => $"{c.Count}x {c.Title}"))
                };

                // Ajouter les tags internes pour lier à l'app
                var internalTags = new List<string>
                {
                    AppService.GetAppInternalTag(appId),
                    "telegram-bot"
                };
                var serverUrl = GetServerUrl();
                var invoiceUrl = $"{serverUrl}/apps/{appId}/pos"; // URL de fallback

                // TODO: Implémenter la création réelle de facture via UIInvoiceController ou API
                // Pour l'instant, retourner l'URL de l'app comme placeholder
                logger.LogInformation("Création de facture pour {Amount} {Currency} - Panier: {CartItems}",
                    amount, currency, JsonConvert.SerializeObject(cartItems));

                using (var context = dbContextFactory.CreateContext())
                {
                    var telegramInvoice = new TelegramBotInvoices
                    {
                        BTCPayInvoiceId = Guid.NewGuid().ToString(), // Remplacer par l'ID réel de la facture
                        StoreId = storeId,
                        AppName = appName,
                        Amount = amount,
                        Currency = currency,
                        DateT = DateTime.UtcNow
                    };
                    context.TelegramInvoices.Add(telegramInvoice);
                    await context.SaveChangesAsync();
                }

                return invoiceUrl;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur lors de la création de la facture");
                return null;
            }
        }


        // Mise à jour de l'inventaire après paiement
        public async Task UpdateInventoryAsync(string appId, Dictionary<string, int> itemQuantities)
        {
            try
            {
                var changes = itemQuantities.Select(kv =>
                    new AppService.InventoryChange(kv.Key, -kv.Value)).ToArray();

                await appService.UpdateInventory(appId, changes);
                logger.LogInformation("Inventaire mis à jour pour l'app {AppId}", appId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur lors de la mise à jour de l'inventaire");
            }
        }
    }
}
