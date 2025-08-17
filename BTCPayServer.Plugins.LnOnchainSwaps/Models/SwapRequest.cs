namespace BTCPayServer.Plugins.LnOnchainSwaps.Models
{
    public class SwapRequest
    {
        public string SwapType { get; set; }
        public decimal BtcAmount { get; set; }
        public bool IsInternal { get; set; }
        public string ExternalAddressOrInvoice { get; set; }
    }
}
