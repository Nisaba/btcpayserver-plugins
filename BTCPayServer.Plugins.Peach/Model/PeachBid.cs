using System.Collections.Generic;

namespace BTCPayServer.Plugins.Peach.Model
{
    public class PeachBid
    {
        public string ID { get; set; }
        public string CountryCode { get; set; }
        public float MinAmount { get; set; }
        public float MaxAmount { get; set; }
        public List<string> PaymentMethods { get; set; }
        public PeachUser User { get; set; }
    }
}
