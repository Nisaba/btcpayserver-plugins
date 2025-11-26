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
using BTCPayServer.Client.Models;
using BTCPayServer.Plugins.Shopstr.Models.External;

namespace BTCPayServer.Plugins.Shopstr.Services
{
    public class ShopstrService (ILogger<ShopstrService> logger)
    {
        private readonly ILogger<ShopstrService> _logger = logger;

        public async Task<List<NostrProduct>> GetShopstrProducts(string merchantPubKeyHex, string[] relays) 
        {
            var products = new List<NostrProduct>();

            var relayUris = relays.Select(r => new Uri(r)).ToArray();
            var client = new CompositeNostrClient(relayUris);
          /*  client.MessageReceived += (s, e) =>
            {
                _logger.LogInformation($"Shopstr Plugin: Nostr message received: {e}");
            };*/
            client.InvalidMessageReceived += (s, e) =>
            {
                _logger.LogWarning($"Shopstr Plugin: Nostr invalid message received: {e}");
            };

            var filter = new NostrSubscriptionFilter()
            {
                Kinds = new[] { 30402 }, //Product definition
                Authors = new[] { merchantPubKeyHex }
            };
            
            try
            {
                await client.ConnectAndWaitUntilConnected(CancellationToken.None);
                var events = new List<NostrEvent>();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var asyncEnumerator = client.SubscribeForEvents([filter], false, cts.Token).GetAsyncEnumerator();

                try
                {
                    while (true)
                    {
                        NostrEvent nostrEvent = null;
                        try
                        {
                            var hasNext = await asyncEnumerator.MoveNextAsync();
                            if (!hasNext)
                                break;

                            nostrEvent = asyncEnumerator.Current;
                            events.Add(nostrEvent);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "MoveNextAsync() error");
                            break;
                        }
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
                //             var events = await client.SubscribeForEvents([filter], false, CancellationToken.None).ToListAsync();
                foreach (var nostrEvent in events)
                {
                    try
                    {
                            products.Add(new NostrProduct
                            {
                                Id = nostrEvent.Id,
                                Name = nostrEvent.GetTaggedData("title")[0],
                                Description = nostrEvent.GetTaggedData("summary")[0],
                                Price = decimal.Parse(nostrEvent.GetTaggedData("price")[0]),
                                Images = nostrEvent.GetTaggedData("image")
                            });

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Shopstr Plugin: Error while parsing Nostr product (Event ID: {nostrEvent.Id})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Shopstr Plugin: Error while parsing Nostr products");
            }
            finally
            {
                client.Dispose();
            }

            return products
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .ToList();
        }

        public async Task CreateShopstrProduct(AppItem appItem, string shopCurrency, Nip5StoreSettings nostrSettings)
        {
            try
            {
                var relayUris = nostrSettings.Relays.Select(r => new Uri(r)).ToArray();
                var nostrEvent = new NostrEvent()
                {
                    PublicKey = nostrSettings.PubKey,
                    Kind = 30402,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Content = appItem.Description,
                };
                nostrEvent.SetTag("d", appItem.Id);
                nostrEvent.SetTag("alt", "Product listing: " + appItem.Title);
                nostrEvent.SetTag("client", ["Shopstr",
                                         "31990:45cb19e028027e0f726459086eafeadd661d0f57486069c81a80e5163538522a:5b4b1e9d6ecaded5a1250ae7117aa5974e50d2e4af3b24d40a125b7e0c0dd68f",
                                         "wss://eden.nostr.land/"]);
                nostrEvent.SetTag("title", appItem.Title);
                nostrEvent.SetTag("summary", appItem.Description);
                nostrEvent.SetTag("price", [appItem.Price.ToString(), shopCurrency]);
                nostrEvent.SetTag("t", "shopstr");
                if (appItem.Categories != null && appItem.Categories.Length > 0)
                    nostrEvent.SetTag("t", appItem.Categories);
                nostrEvent.SetTag("status", appItem.Disabled ? "sold" : "active");
                nostrEvent.SetTag("image", appItem.Image);

                var ecPrivKey = ECPrivKey.Create(Convert.FromHexString(nostrSettings.PrivateKey));
                nostrEvent.Signature = NostrExtensions.ComputeSignature(nostrEvent, ecPrivKey);

                using (var client = new CompositeNostrClient(relayUris))
                {
                    await client.PublishEvent(nostrEvent, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Shopstr Plugin: Error while creating Nostr product");
                throw;
            }
        }
    }
}
