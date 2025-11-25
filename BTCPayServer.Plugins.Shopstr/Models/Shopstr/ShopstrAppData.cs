using BTCPayServer.Client.Models;
using BTCPayServer.Data;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using System.IO;

namespace BTCPayServer.Plugins.Shopstr.Models.Shopstr
{
    public class ShopstrAppData : AppData
    {
        public string CurrencyCode { get; set; }
        public List<AppItem> ShopItems { get; set; }

        public List<string> ItemsToAdd { get; set; }
        public List<string> ItemsToRemove { get; set; }
        public List<string> ItemsToUpdate { get; set; }

        public void GetListItemsToSync(List<ShopAppStoreItem> AppSentItemsToShopstr)
        {
            ItemsToAdd = new List<string>();
            ItemsToRemove = new List<string>();
            ItemsToUpdate = new List<string>();
            foreach (var item in ShopItems)
            {
                var hash = GetHashItem(item);
                var sentItem = AppSentItemsToShopstr.Find(i => i.ItemId == item.Id);
                if (sentItem == null && !item.Disabled)
                {
                    ItemsToAdd.Add(item.Id);
                }
                else if (sentItem.Hash != hash)
                {
                    ItemsToUpdate.Add(item.Id);
                }
            }
            foreach (var sentItem in AppSentItemsToShopstr)
            {
                var item = ShopItems.Find(i => i.Id == sentItem.ItemId);
                if (item == null || item.Disabled)
                {
                    ItemsToRemove.Add(sentItem.ItemId);
                }
            }
        }

        public static string GetHashItem(AppItem item)
        {
                var sToHash = $"{item.Title ?? ""}|{item.Description ?? ""}|{string.Join("|", item.Categories ?? [])}|{item.Image ?? ""}|{item.PriceType}|{item.Price ?? 0}|{item.BuyButtonText ?? ""}|{item.Inventory ?? 0}";
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sToHash));
                return System.BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
