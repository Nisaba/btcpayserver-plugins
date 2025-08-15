namespace BTCPayServer.Plugins.LnOnchainSwaps.Models
{
    public class LnOnchainSwapsOperation
    {
        public decimal Amount { get; set; }
        public bool IsOnChain { get; set; }
        public string LnInvoice { get; set; }
        public string BtcDestAdress { get; set; }
    }
}
