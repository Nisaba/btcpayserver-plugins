using System.Collections.Generic;

namespace BTCPayServer.Plugins.B2PCentral.Models
{
    public class B2POffer
    {
        public string ID { get; set; }
        public string CountryCode { get; set; }
        public string CurrencyCode { get; set; }
        public float Price { get; set; }
        public float MinAmount { get; set; }
        public float MaxAmount { get; set; }
        public List<string> PaymentMethods { get; set; }
        public string UserName { get; set; }
        public uint UserNbTrades { get; set; }
        public string Url { get; set; }
        public ProvidersEnum NumProvider { get; set; }
    }
}
