using BTCPayServer.Client.Models;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.Shopstr.Models.Nostr
{
    public class NostrProduct
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public string Image { get; set; }
        public bool Status { get; set; }

        public bool Compare(AppItem appItem) { 
            return Id == appItem.Id &&
                   Name == appItem.Title &&
                   Description == appItem.Description &&
                   Price == appItem.Price &&
                   Image.Contains(appItem.Image.Substring(1)) &&
                   Status != appItem.Disabled;
        }
    }
}
