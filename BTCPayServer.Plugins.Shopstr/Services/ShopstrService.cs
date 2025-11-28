using BTCPayServer.Client.Models;
using BTCPayServer.Plugins.Shopstr.Models.External;
using BTCPayServer.Plugins.Shopstr.Models.Nostr;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NBitcoin.Secp256k1;
using Newtonsoft.Json;
using NNostr.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
           /* client.MessageReceived += (s, e) =>
            {
                _logger.LogInformation($"Shopstr Plugin: Message received: {e}");
            };
            client.InvalidMessageReceived += (s, e) =>
            {
                _logger.LogWarning($"Shopstr Plugin: Invalid message: {e}");
            };*/

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

                events.RemoveAll(e =>
                {
                    var clientTag = e.Tags?.FirstOrDefault(tag => tag.TagIdentifier == "client");
                    if (clientTag == null)
                        return true;
                    return !clientTag.Data?.Any(data => data?.Contains("BTCPayServer-Shopstr", StringComparison.OrdinalIgnoreCase) == true) ?? true;
                });

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
                                Status = nostrEvent.GetTaggedData("status")[0] == "active",
                                Image = nostrEvent.GetTaggedData("image")[0]
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

        public async Task CreateShopstrProduct(AppItem appItem, string shopCurrency, Nip5StoreSettings nostrSettings, string baseUrl, bool bUnpublished = false)
        {
            try
            {
                var relayUris = nostrSettings.Relays.Select(r => new Uri(r)).ToArray();
                var nostrEvent = new NostrEvent()
                {
                    PublicKey = nostrSettings.PubKey,
                    Kind = 30402,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Content = appItem.Description ?? "",
                };
                nostrEvent.SetTag("d", appItem.Id);
                nostrEvent.SetTag("client", ["BTCPayServer-Shopstr",
                             $"31990:{nostrSettings.PubKey}:btcpayserver-shopstr",
                             relayUris[0].ToString()]);

                if (bUnpublished)
                {
                    nostrEvent.SetTag("title", "[UNPUBLISHED] " + appItem.Title);
                    nostrEvent.SetTag("status", "deleted");
                } else
                {
                    nostrEvent.SetTag("title", appItem.Title);
                    nostrEvent.SetTag("alt", "Product listing: " + appItem.Title);
                    nostrEvent.SetTag("summary", appItem.Description ?? "");
                    nostrEvent.SetTag("price", [appItem.Price.ToString(), shopCurrency]);
                    nostrEvent.SetTag("t", "shopstr");
                    if (appItem.Categories != null && appItem.Categories.Length > 0)
                        nostrEvent.SetTag("t", appItem.Categories);
                    nostrEvent.SetTag("status", appItem.Disabled || appItem.Inventory == 0 ? "sold" : "active");
                    var imageUrl = appItem.Image;

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        if (imageUrl.StartsWith("~/", StringComparison.Ordinal))
                        {
                            imageUrl = imageUrl.Substring(1);
                        }

                        if (!imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            imageUrl = baseUrl.TrimEnd('/') + imageUrl;
                        }
                        nostrEvent.SetTag("image", imageUrl);
                    }
                }
                
                var tagsForSerialization = nostrEvent.Tags
                    .Select(tag => tag.TagIdentifier == null 
                        ? tag.Data.ToArray() 
                        : new[] { tag.TagIdentifier }.Concat(tag.Data ?? Enumerable.Empty<string>()).ToArray())
                    .ToList();

                var createdAt = (nostrEvent.CreatedAt ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();

                // Sérialisation selon NIP-01 : [0, pubkey, created_at, kind, tags, content]
                var eventData = new object[]
                {
                    0,
                    nostrEvent.PublicKey,
                    createdAt,
                    nostrEvent.Kind,
                    tagsForSerialization,
                    nostrEvent.Content ?? ""
                };

                var serialized = JsonConvert.SerializeObject(eventData, 
                    Formatting.None, 
                    new JsonSerializerSettings 
                    { 
                        NullValueHandling = NullValueHandling.Include 
                    });

                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(serialized));
                nostrEvent.Id = Convert.ToHexString(hash).ToLowerInvariant();
                
                var ecPrivKey = ECPrivKey.Create(Convert.FromHexString(nostrSettings.PrivateKey));
                nostrEvent.Signature = NostrExtensions.ComputeSignature(nostrEvent, ecPrivKey);

                using (var client = new CompositeNostrClient(relayUris))
                {
                   /* client.MessageReceived += (s, e) =>
                    {
                        _logger.LogInformation($"Shopstr Plugin: Message received: {e}");
                    };
                    client.InvalidMessageReceived += (s, e) =>
                    {
                        _logger.LogWarning($"Shopstr Plugin: Invalid message: {e}");
                    };*/

                    await client.ConnectAndWaitUntilConnected(CancellationToken.None);
                    //_logger.LogInformation($"Shopstr Plugin: Connected to {relayUris.Length} relay(s)");

                    await client.PublishEvent(nostrEvent, CancellationToken.None);
                    //_logger.LogInformation($"Shopstr Plugin: Event {nostrEvent.Id} published to relay(s)");

                    await Task.Delay(TimeSpan.FromSeconds(2));

                    var filter = new NostrSubscriptionFilter()
                    {
                        Ids = new[] { nostrEvent.Id }
                    };

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    var verification = await client.SubscribeForEvents([filter], false, cts.Token).FirstOrDefaultAsync();

                    if (verification != null)
                    {
                        _logger.LogInformation($"Shopstr Plugin: Event {nostrEvent.Id} confirmed on relay(s)");
                    }
                    else
                    {
                        _logger.LogWarning($"Shopstr Plugin: Event {nostrEvent.Id} could not be verified on relay(s)");
                    }
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
