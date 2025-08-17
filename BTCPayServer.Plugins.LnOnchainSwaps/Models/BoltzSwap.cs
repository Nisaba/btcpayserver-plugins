using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Models
{
    public class BoltzSwap
    {
        public const string SwapTypeOnChainToLn = "onchain_to_ln";
        public const string SwapTypeLnToOnChain = "ln_to_onchain";

        [Key]
        public string SwapId { get; set; }
        public string StoreId { get; set; }

        public string Type { get; set; } // "onchain_to_ln" or "ln_to_onchain"
        public string PreImageHash { get; set; }
        public string Destination { get; set; } // BtcAddress or LnInvoice
        public decimal ExpectedAmount { get; set; }
        public string BTCPayPayoutId { get; set; }
        public string Json { get; set; }
    }
}
