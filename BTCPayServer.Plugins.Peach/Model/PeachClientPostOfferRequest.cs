namespace BTCPayServer.Plugins.Peach.Model
{
    public class PeachClientPostOfferRequest
    {
        public string Token { get; set; }
        public decimal Amount { get; set; }
        public decimal Premium { get; set; }
        public string Currency { get; set; }
    }
}
