using BTCPayServer.Client.Models;
using BTCPayServer.Plugins.Shopstr.Models.Shopstr;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.Shopstr.Models.Nostr
{
    public class NostrProduct
    {
        public string Id { get; set; }
        public string[] Categories { get; set; }
        public string Location { get; set; }
        public ConditionEnum Condition { get; set; }
        public DateTimeOffset? ValidDateT { get; set; }
        public string Restrictions { get; set; }

        public int Qty { get; set; }
        public int TimeStamp { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public string Image { get; set; }
        public bool Status { get; set; }

        public bool Compare(AppItem appItem, ShopstrAppData app) { 
            return Id == appItem.Id &&
                   Name == appItem.Title &&
                   Description == appItem.Description &&
                   CategoriesEqual(Categories, appItem.Categories) &&
                   Location == app.Location &&
                   Condition == app.Condition &&
                   ValidDateT?.Date == app.ValidDateT?.Date &&
                   Restrictions == app.Restrictions &&
                   Qty == appItem.Inventory &&
                   Price == appItem.Price &&
                   Image.Contains(appItem.Image.Substring(1)) &&
                   Status != appItem.Disabled;
        }

        private bool CategoriesEqual(string[] categories1, string[] categories2)
        {
            var isEmpty1 = categories1 == null || categories1.Length == 0;
            var isEmpty2 = categories2 == null || categories2.Length == 0;

            if (isEmpty1 && isEmpty2)
                return true;

            if (isEmpty1 || isEmpty2)
                return false;

            return categories1.Length == categories2.Length &&
                   new HashSet<string>(categories1).SetEquals(categories2);
        }
    }
}
