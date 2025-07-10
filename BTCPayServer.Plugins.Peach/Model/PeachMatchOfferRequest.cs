namespace BTCPayServer.Plugins.Peach.Model
{
    public class PeachMatchOfferRequest
    {
        public string PeachToken { get; set; }
        public string OfferId { get; set; }
        public string MatchingOfferId { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Price { get; set; }
        public decimal Premium { get; set; }
    }
}
