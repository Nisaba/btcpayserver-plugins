using System.Collections.Generic;

namespace BTCPayServer.Plugins.B2PCentral.Models.Swaps
{
    public class B2PSwapResponse
    {
        public List<B2PSwap> Swaps { get; set; } = new List<B2PSwap>();

    }
}
