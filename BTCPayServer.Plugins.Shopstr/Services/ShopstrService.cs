using Amazon.Auth.AccessControlPolicy;
using BTCPayServer.Client.Models;
using BTCPayServer.Plugins.Shopstr.Models;
using BTCPayServer.Plugins.Shopstr.Models.External;
using BTCPayServer.Plugins.Shopstr.Models.Nostr;
using BTCPayServer.Plugins.Shopstr.Models.Shopstr;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NBitcoin.Secp256k1;
using Newtonsoft.Json;
using NNostr.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Shopstr.Services
{
    public class ShopstrService (ILogger<ShopstrService> logger)
    {
        private CompositeNostrClient _client;

        public async Task InitializeClient(string[] relayUrls)
        {
            try {
                var relayUris = relayUrls.Select(r => new Uri(r)).ToArray();
                _client = new CompositeNostrClient(relayUris);
                /* _client.MessageReceived += (s, e) =>
                 {
                     _logger.LogInformation($"Shopstr Plugin: Message received: {e}");
                 };
                 _client.InvalidMessageReceived += (s, e) =>
                 {
                     _logger.LogWarning($"Shopstr Plugin: Invalid message: {e}");
                 };*/
                await _client.ConnectAndWaitUntilConnected(CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Shopstr Plugin: Error while opening Nostr connection");
            }
        }

        public void DisposeClient()
        {
            try
            {
                _client?.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Shopstr Plugin: Error while disposing Nostr connection");
            }
        }

        public async Task<List<NostrProduct>> GetShopstrProducts(string merchantPubKeyHex) 
        {
            var products = new List<NostrProduct>();

            var filter = new NostrSubscriptionFilter()
            {
                Kinds = new[] { 30402 }, //Product definition
                Authors = new[] { merchantPubKeyHex }
            };
            
            try
            {
                var events = new List<NostrEvent>();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var asyncEnumerator = _client.SubscribeForEvents([filter], false, cts.Token).GetAsyncEnumerator();

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
                            logger.LogError(ex, "MoveNextAsync() error");
                            break;
                        }
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }

                // var serializedEvent = JsonConvert.SerializeObject(events[0], Formatting.Indented);
                // _logger.LogInformation($"Shopstr Plugin: First event received: {serializedEvent}");

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
                                Id = nostrEvent.GetTaggedData("d")[0],
                                Name = nostrEvent.GetTaggedData("title")[0],
                                Description = nostrEvent.GetTaggedData("summary")?.FirstOrDefault() ?? "",
                                Categories = nostrEvent.GetTaggedData("t")?.Where(t => t != "shopstr").ToArray() ?? Array.Empty<string>(),
                                Location = nostrEvent.GetTaggedData("location")?.FirstOrDefault() ?? "",
                                Condition = Enum.TryParse<ConditionEnum>(nostrEvent.GetTaggedData("condition")?.FirstOrDefault(), out var condition) ? condition : ConditionEnum.None,
                                Restrictions = nostrEvent.GetTaggedData("restrictions")?.FirstOrDefault() ?? "",
                                ValidDateT = nostrEvent.GetTaggedData("valid_until") != null &&
                                             long.TryParse(nostrEvent.GetTaggedData("valid_until").FirstOrDefault(), out var validUntilUnix)
                                    ? DateTimeOffset.FromUnixTimeSeconds(validUntilUnix)
                                    : null,
                                TimeStamp = int.TryParse(nostrEvent.GetTaggedData("published_at")?.FirstOrDefault(), out var timestamp) ? timestamp : 0,
                                Qty = int.TryParse(nostrEvent.GetTaggedData("quantity")?.FirstOrDefault(), out var qty) ? qty : 0,
                                Price = decimal.TryParse(nostrEvent.GetTaggedData("price")?.FirstOrDefault(), out var price) ? price : 0,
                                Status = nostrEvent.GetTaggedData("status")[0] == "active",
                                Image = nostrEvent.GetTaggedData("image")?.FirstOrDefault() ?? ""
                            });

                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Shopstr Plugin: Error while parsing Nostr product (Event ID: {nostrEvent.Id})");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Shopstr Plugin: Error while parsing Nostr products");
            }

            return products
                .OrderByDescending(p => p.TimeStamp)
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .ToList();
        }

        public async Task CreateShopstrProduct(AppItem appItem, ShopstrAppData appData, Nip5StoreSettings nostrSettings, string baseUrl, bool bUnpublished = false)
        {
            try
            {
                var relayUris = nostrSettings.Relays.Select(r => new Uri(r)).ToArray();
                var dtNow = DateTimeOffset.UtcNow;
                var nostrEvent = new NostrEvent()
                {
                    PublicKey = nostrSettings.PubKey,
                    Kind = 30402,
                    CreatedAt = dtNow,
                    Content = appItem.Description ?? "",
                };
                nostrEvent.SetTag("d", appItem.Id);
                nostrEvent.SetTag("client", ["BTCPayServer-Shopstr",
                             $"31990:{nostrSettings.PubKey}:btcpayserver-shopstr",
                             relayUris[0].ToString()]);

                string imageUrl = appItem.Image;
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
                }

                if (bUnpublished)
                {
                    nostrEvent.SetTag("title", "[UNPUBLISHED] " + appItem.Title);
                    nostrEvent.SetTag("status", "deleted");

                    //var addressableTag = $"{nostrEvent.Kind}:{nostrEvent.PublicKey}:{appItem.Id}";
                    //await PublishDeletionEvent(nostrSettings, aTags: new[] { addressableTag });

                    if (appData.FlashSales)
                    {
                        await PublishFlashSaleEvent(appItem, appData, nostrSettings, dtNow, imageUrl, true);
                    }
                }
                else
                {
                    nostrEvent.SetTag("title", appItem.Title);
                    nostrEvent.SetTag("alt", "Product listing: " + appItem.Title);
                    nostrEvent.SetTag("summary", appItem.Description ?? "");
                    nostrEvent.SetTag("price", [appItem.Price.ToString(), appData.CurrencyCode]);
                    nostrEvent.SetTag("location", appData.Location);
                    nostrEvent.SetTag("t", "shopstr");
                    if (appItem.Categories != null && appItem.Categories.Length > 0)
                        nostrEvent.SetTag("t", appItem.Categories);
                    nostrEvent.SetTag("status", appItem.Disabled || appItem.Inventory == 0 ? "sold" : "active");
                    nostrEvent.SetTag("published_at", dtNow.ToUnixTimeSeconds().ToString());

                    if (appData.Condition != ConditionEnum.None)
                    {
                        var field = typeof(ConditionEnum).GetField(appData.Condition.ToString());
                        var descAttr = (DescriptionAttribute)field.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
                        var displayCondition = descAttr?.Description ?? appData.Condition.ToString();
                        nostrEvent.SetTag("condition", displayCondition);
                    }
                    if (!string.IsNullOrEmpty(appData.Restrictions))
                    {
                        nostrEvent.SetTag("restrictions", appData.Restrictions);
                    }
                    if (appData.ValidDateT.HasValue)
                    {
                        nostrEvent.SetTag("valid_until", appData.ValidDateT.Value.ToUnixTimeSeconds().ToString());
                    }
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        nostrEvent.SetTag("image", imageUrl);
                    }

                    if (appData.FlashSales && !(appItem.Disabled || appItem.Inventory == 0))
                    {
                        await PublishFlashSaleEvent(appItem, appData, nostrSettings, dtNow, imageUrl, false);
                    }
                }

                await SignAndPublishEvent(nostrEvent, nostrSettings.PrivateKey);

                await Task.Delay(TimeSpan.FromSeconds(2));

                var filter = new NostrSubscriptionFilter()
                {
                    Ids = new[] { nostrEvent.Id }
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var verification = await _client.SubscribeForEvents([filter], false, cts.Token).FirstOrDefaultAsync();

                if (verification == null)
                {
                    logger.LogWarning($"Shopstr Plugin: Event {nostrEvent.Id} could not be verified on relay(s)");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Shopstr Plugin: Error while creating Nostr product");
                throw;
            }
        }

         private async Task PublishFlashSaleEvent(AppItem appItem, ShopstrAppData appData, Nip5StoreSettings nostrSettings, DateTimeOffset dtNow, string imageUrl, bool isUnpublish)
        {
            try
            {
                // NIP-09 retrieval for deletion
                if (isUnpublish)
                {
                    try 
                    {
                        var filter = new NostrSubscriptionFilter()
                        {
                            Kinds = new[] { 1 },
                            Authors = new[] { nostrSettings.PubKey },
                            Limit = 5 
                        };
                        using var ctsSearch = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        var events = await _client.SubscribeForEvents([filter], false, ctsSearch.Token).ToListAsync();
                        
                        var zapsnagToDelete = events
                            .Where(e => e.GetTaggedData("d")?.Contains("zapsnag") == true)
                            .OrderByDescending(e => e.CreatedAt)
                            .FirstOrDefault();

                        if (zapsnagToDelete != null)
                        {
                            await PublishDeletionEvent(nostrSettings, eTags: new[] { zapsnagToDelete.Id });
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Shopstr Plugin: Could not fetch previous flash sale event for deletion");
                    }
                    return;
                }

                var flashEvent = new NostrEvent()
                {
                    PublicKey = nostrSettings.PubKey,
                    Kind = 1,
                    CreatedAt = dtNow,
                    Content = BuildFlashSaleContent(appItem, appData, isUnpublish, imageUrl),
                };

                flashEvent.SetTag("t", "zapsnag");
                flashEvent.SetTag("t", "shopstr-zapsnag");
                flashEvent.SetTag("d", "zapsnag");
                flashEvent.SetTag("status", isUnpublish ? "deleted" : "active");

                if (!isUnpublish) 
                {
                    if (!string.IsNullOrEmpty(imageUrl))
                        flashEvent.SetTag("image", imageUrl);
                    if (appItem?.Inventory != null && appItem?.Inventory > 0)
                        flashEvent.SetTag("quantity", appItem.Inventory.ToString());
                }

                await SignAndPublishEvent(flashEvent, nostrSettings.PrivateKey);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, isUnpublish ? "Shopstr Plugin: Error while unpublishing flash sale (zapsnag) event" : "Shopstr Plugin: Error while publishing flash sale (zapsnag) event");
            }
        }

        private async Task PublishDeletionEvent(Nip5StoreSettings nostrSettings, string[] eTags = null, string[] aTags = null)
        {
            try
            {
                var deleteEvent = new NostrEvent()
                {
                    PublicKey = nostrSettings.PubKey,
                    Kind = 5,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Content = "Item unpublish request"
                };

                if (eTags != null)
                {
                    foreach (var id in eTags)
                    {
                        deleteEvent.SetTag("e", id);
                    }
                }

                if (aTags != null)
                {
                    foreach (var addr in aTags)
                    {
                        deleteEvent.SetTag("a", addr);
                    }
                }

                await SignAndPublishEvent(deleteEvent, nostrSettings.PrivateKey);
                logger.LogInformation("Shopstr Plugin: Deletion event (Kind 5) published.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Shopstr Plugin: Error while publishing deletion event");
            }
        }

        private async Task SignAndPublishEvent(NostrEvent nostrEvent, string privateKeyHex)
        {
            var tagsForSerialization = nostrEvent.Tags
                  .Select(tag => tag.TagIdentifier == null
                      ? tag.Data.ToArray()
                      : new[] { tag.TagIdentifier }.Concat(tag.Data ?? Enumerable.Empty<string>()).ToArray())
                  .ToList();

            var createdAt = (nostrEvent.CreatedAt ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();

            // Serilization NIP-01
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

            var ecPrivKey = ECPrivKey.Create(Convert.FromHexString(privateKeyHex));
            nostrEvent.Signature = NostrExtensions.ComputeSignature(nostrEvent, ecPrivKey);

            await _client.PublishEvent(nostrEvent, CancellationToken.None);
        }

        private string BuildFlashSaleContent(AppItem appItem, ShopstrAppData appData, bool isUnpublish, string imageUrl)
        {
            var statusText = isUnpublish ? "REMOVED FROM SALE" : "FLASH SALE";
            var currencySymbol = appData.CurrencyCode; // Assuming this contains the currency symbol
            var priceInfo = isUnpublish ? "" : $"Price: {appItem.Price} {currencySymbol}";

            return string.Concat(
                $"{appItem.Title}",
                $"\n{statusText}",
                $"\n{priceInfo}",
                $"\n\n#zapsnag",
                $"{(string.IsNullOrEmpty(imageUrl) ? "" : "\n" + imageUrl)}"
            );
        }
    }
}
