
using System.Collections.Generic;

namespace BTCPayServer.Plugins.B2PCentral.Models
{
    public class B2PViewModel
    {
        public B2PSettings Settings { get; set; }
        public List<B2PStoreSwap> Swaps { get; set; }
    }
}
