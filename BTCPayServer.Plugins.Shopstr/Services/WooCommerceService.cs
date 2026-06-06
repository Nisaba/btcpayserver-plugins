using BTCPayServer.Client.Models;
using BTCPayServer.Plugins.Shopstr.Models;
using BTCPayServer.Plugins.Shopstr.Models.Shopstr;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Shopstr.Services
{
    public class WooCommerceService(ILogger<WooCommerceService> logger)
    {
        public async Task<ShopstrAppData> FetchProducts(WooCommerceSettings settings)
        {
            var products = new List<AppItem>();
            var baseUrl = settings.WooCommerceUrl.TrimEnd('/');
            var page = 1;
            var perPage = 100;

            using var client = new HttpClient();
            var authBytes = Encoding.ASCII.GetBytes($"{settings.ConsumerKey}:{settings.ConsumerSecret}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            while (true)
            {
                var url = $"{baseUrl}/wp-json/wc/v3/products?status=publish&per_page={perPage}&page={page}";

                try
                {
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorText = await response.Content.ReadAsStringAsync();
                        logger.LogError("WooCommerce API error: {Status} - {Error}", response.StatusCode, errorText);
                        break;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var wcProducts = JsonConvert.DeserializeObject<List<WcProduct>>(json);

                    if (wcProducts == null || wcProducts.Count == 0)
                        break;

                    foreach (var wc in wcProducts)
                    {
                        products.Add(new AppItem
                        {
                            Id = wc.Id.ToString(),
                            Title = wc.Name,
                            Description = StripHtml(wc.ShortDescription ?? wc.Description ?? ""),
                            Price = decimal.TryParse(wc.Price, out var p) ? p : 0,
                            Image = wc.Images?.FirstOrDefault()?.Src ?? "",
                            Disabled = wc.Status != "publish",
                            Inventory = wc.StockQuantity ?? (wc.InStock ? 999 : 0),
                            Categories = wc.Categories?.Select(c => c.Name).ToArray() ?? Array.Empty<string>(),
                        });
                    }

                    if (wcProducts.Count < perPage)
                        break;

                    page++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to fetch WooCommerce products (page {Page})", page);
                    break;
                }
            }

            logger.LogInformation("Fetched {Count} products from WooCommerce", products.Count);

            return new ShopstrAppData
            {
                Id = "woocommerce",
                Name = "WooCommerce",
                CurrencyCode = "USD",
                Location = settings.Location ?? "",
                FlashSales = settings.FlashSales,
                Condition = settings.Condition,
                Restrictions = settings.Restrictions ?? "",
                ShopItems = products
            };
        }

        private static string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", "").Trim();
        }

        private class WcProduct
        {
            [JsonProperty("id")] public int Id { get; set; }
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("status")] public string Status { get; set; }
            [JsonProperty("description")] public string Description { get; set; }
            [JsonProperty("short_description")] public string ShortDescription { get; set; }
            [JsonProperty("price")] public string Price { get; set; }
            [JsonProperty("stock_quantity")] public int? StockQuantity { get; set; }
            [JsonProperty("in_stock")] public bool InStock { get; set; }
            [JsonProperty("images")] public List<WcImage> Images { get; set; }
            [JsonProperty("categories")] public List<WcCategory> Categories { get; set; }
        }

        private class WcImage
        {
            [JsonProperty("src")] public string Src { get; set; }
        }

        private class WcCategory
        {
            [JsonProperty("name")] public string Name { get; set; }
        }
    }
}
