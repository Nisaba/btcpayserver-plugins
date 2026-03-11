namespace BTCPayServer.Plugins.Exolix.Model
{
    public class SwapRequest
    {
        public string CryptoFrom { get; set; }
        public string BtcAddress { get; set; }
        public decimal BtcAmount { get; set; }
        public string BtcPayInvoiceId { get; set; }
    }
}
