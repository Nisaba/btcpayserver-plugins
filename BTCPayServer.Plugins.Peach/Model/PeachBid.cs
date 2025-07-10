using System.Collections.Generic;

namespace BTCPayServer.Plugins.Peach.Model
{
    public class PeachBid
    {
        public string Id { get; set; }
        public float MinAmount { get; set; }
        public float MaxAmount { get; set; }
        public float MinFiatAmount { get; set; }
        public float MaxFiatAmount { get; set; }
        public List<string> PaymentMethods { get; set; }
        public PeachUser User { get; set; }
     //   public bool IsOnline { get; set; }
    }
}
