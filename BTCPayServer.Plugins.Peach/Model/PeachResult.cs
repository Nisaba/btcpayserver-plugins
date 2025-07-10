using System.Collections.Generic;

namespace BTCPayServer.Plugins.Peach.Model
{
    public class PeachResult
    {
        public string ErrorMsg { get; set; }

        public List<PeachBid> Bids { get; set; }
    }
}
