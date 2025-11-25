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
        public string[] Images { get; set; }
    }
}
