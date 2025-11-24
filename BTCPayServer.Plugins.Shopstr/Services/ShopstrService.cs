using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.Secp256k1; 
using NNostr.Client;
using Newtonsoft.Json;
using BTCPayServer.Plugins.Shopstr.Models.Nostr;
using Microsoft.Extensions.Logging;
using AngleSharp.Dom.Events;

namespace BTCPayServer.Plugins.Shopstr.Services
{
    public class ShopstrService (ILogger<ShopstrService> logger)
    {
        private readonly ILogger<ShopstrService> _logger = logger;

        public async Task<List<NostrProduct>> GetMerchantProductsAsync(
                string merchantPubKeyHex,
                string[] relays) 
        {
            var products = new List<NostrProduct>();

            var relayUris = relays.Select(r => new Uri(r)).ToArray();
            var client = new CompositeNostrClient(relayUris);

            var filter = new NostrSubscriptionFilter()
            {
                Kinds = new[] { 30402 }, //Product definition
                Authors = new[] { merchantPubKeyHex }
            };

            try
            {
                await client.ConnectAndWaitUntilConnected(CancellationToken.None);

                var events = client.SubscribeForEvents([filter], true, CancellationToken.None);

                await foreach (var nostrEvent in events)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(nostrEvent.Content)) continue;

                        var content = JsonConvert.DeserializeObject<ProductContent>(nostrEvent.Content);

                        if (content != null)
                        {
                            products.Add(new NostrProduct
                            {
                                Id = nostrEvent.Id,
                                Name = content.Name,
                                Description = content.Description,
                                Price = content.Price,
                                Currency = content.Currency,
                                Images = content.Images
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error while parsing Nostr product (Event ID: {nostrEvent.Id})");
                    }
                }
            }
            finally
            {
                client.Dispose();
            }

            return products;
        }
    }
}
