using BTCPayServer.Client;
using BTCPayServer.Client.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Ecwid.Services
{
    public class BtcPayService(ILogger<BtcPayService> logger, BTCPayServerClient client)
    {
        private readonly ILogger<BtcPayService> _logger = logger;
        private readonly BTCPayServerClient _client = client;

        public bool CheckSecretKey(string key, string message, string signature)
        {
            var msgBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(message));
            string hashString = string.Empty;
            foreach (byte x in msgBytes)
            {
                hashString += String.Format("{0:x2}", x);
            }
            return (hashString == signature);
        }

        public async Task<string> CreateWebHook(string webHookUrl, string storeId)
        {
            var existing = await client.GetWebhooks(storeId);
            var existingWebHook = existing.Where(x => x.Url == webHookUrl);
            foreach (var webhookData in existingWebHook)
            {
                await client.DeleteWebhook(storeId, webhookData.Id);
            }

            var response = await _client.CreateWebhook(storeId,
                new CreateStoreWebhookRequest()
                {
                    Url = webHookUrl,
                    Enabled = true,
                    AuthorizedEvents = new StoreWebhookBaseData.AuthorizedEventsData()
                    {
                        SpecificEvents = new[]
                        {
                            WebhookEventType.InvoiceReceivedPayment, WebhookEventType.InvoiceProcessing,
                            WebhookEventType.InvoiceExpired, WebhookEventType.InvoiceSettled,
                            WebhookEventType.InvoiceInvalid, WebhookEventType.InvoicePaymentSettled,
                        }
                    }
                });
            return response.Secret;
        }


    }
}
