namespace BTCPayServer.Plugins.LnOnchainSwaps.Models
{
    public class LnToOnChainSwap
    {
        public bool ToInternalOnChainWallet { get; set; }

        public string ExternalOnChainAddress { get; set; }

        public decimal BtcAmount { get; set; }

    }
}
