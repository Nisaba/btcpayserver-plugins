using BTCPayServer.Plugins.B2PCentral.Models.P2P;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.B2PCentral.Models.Swaps
{
    public class B2PSwapResult
    {
        public string ErrorMsg { get; set; }
        public string ToCrypto { get; set; }
        public string ToNetwork { get; set; }
        public string FiatCurrency { get; set; }
        public SwapRateRequest RateRequest { get; set; }

        public string UserEmail { get; set; }
        public List<B2PSwap> Swaps { get; set; }
    }
}
