namespace BTCPayServer.Plugins.LnOnchainSwaps.Models
{
    public class OnChainToLnSwap
    {
        public bool ToInternalLnWalet { get; set; }

        public string ExternalLnInvoice { get; set; }

        public decimal BtcAmount { get; set; }

    }
}
