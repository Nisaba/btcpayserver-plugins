namespace BTCPayServer.Plugins.Peach.Model
{
    public class PeachRequest
    {
        public string Token { get; set; }
        public string CurrencyCode { get; set; }
        public decimal BtcAmount { get; set; }
        public decimal Rate { get; set; }
    }
}
