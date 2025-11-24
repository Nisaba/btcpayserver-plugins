using Newtonsoft.Json;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.Shopstr.Models.Nostr
{
    //JSON template contained in the "Content" field of event 30402
    public class ProductContent
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("price")]
        public decimal Price { get; set; }
        [JsonProperty("currency")]
        public string Currency { get; set; }
        [JsonProperty("images")]
        public List<string> Images { get; set; }
    }
}
