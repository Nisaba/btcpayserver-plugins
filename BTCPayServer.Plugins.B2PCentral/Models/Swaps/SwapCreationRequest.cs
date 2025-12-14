namespace BTCPayServer.Plugins.B2PCentral.Models.Swaps
{
    public class SwapCreationCommon
    {
        public SwapProvidersEnum Provider { get; set; }
        public string QuoteID { get; set; }
        public string ToCrypto { get; set; }
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
        public string ToAddress { get; set; }
        public string FromRefundAddress { get; set; }
        public bool IsFixed { get; set; }
        public string NotificationEmail { get; set; }
    }

    public class SwapCreationRequestJS : SwapCreationCommon
    {
        public string ApiKey { get; set; }
    }

    public class SwapCreationRequest : SwapCreationCommon
    {
        public string FromCrypto { get; set; }
        public string FromNetwork { get; set; }
        public string ToNetwork { get; set; }
        public string NotificationNpub { get; set; }
    }
}
