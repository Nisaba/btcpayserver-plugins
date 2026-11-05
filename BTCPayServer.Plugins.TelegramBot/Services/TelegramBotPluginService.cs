using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Client;
using BTCPayServer.Client.Models;
using BTCPayServer.Data;
using BTCPayServer.Plugins.TelegramBot.Data;
using BTCPayServer.Plugins.TelegramBot.Models;
using BTCPayServer.Security.Greenfield;
using BTCPayServer.Services.Apps;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.DataEncoders;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Plugins.TelegramBot.Services
{
    public class TelegramBotPluginService(AppService appService,
                                          ILoggerFactory loggerFactory,
                                          TelegramBotDbContextFactory dbContextFactory,
                                          APIKeyRepository apiKeyRepository)
    {
        private readonly ILogger<TelegramBotPluginService> logger = loggerFactory.CreateLogger<TelegramBotPluginService>();
        public ConcurrentBag<TelegramBot> telegramBots = new();

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
                using var context = dbContextFactory.CreateContext();
                var config = await context.Config.FirstOrDefaultAsync();
                if (config == null)
                {
                    var keyBytes = new byte[20];
                    RandomNumberGenerator.Fill(keyBytes);
                    var apiKey = Encoders.Hex.EncodeData(keyBytes);
                    
                    var hash = Encoders.Hex.EncodeData(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));
                    var idHash = SHA256.HashData(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));
                    var id = APIKeyData.IdPrefix + "_" + Encoders.Hex.EncodeData(idHash)[..16];

                    var apiKeyData = new APIKeyData
                    {
                        Id = id,
                        Key = apiKey,
                        Hash = hash,
                        Prefix = apiKey.Substring(0, 6),
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
            catch (Exception e)
            {
                logger.LogError(e, "TelegramBotPlugin:InitBaseUrl()");
