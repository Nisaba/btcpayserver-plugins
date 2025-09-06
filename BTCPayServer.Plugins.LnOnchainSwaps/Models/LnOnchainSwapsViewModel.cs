using System.Collections.Generic;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Models
{
    public class LnOnchainSwapsViewModel
    {
        public string StoreId;
        public bool HasPrivateKey;
        public List<BoltzSwap> Swaps { get; set; }
        public bool IsPayoutCreated { get; set; }
    }
}
