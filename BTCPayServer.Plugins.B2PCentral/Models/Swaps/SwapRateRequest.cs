namespace BTCPayServer.Plugins.B2PCentral.Models.Swaps
{
    public class SwapRateCommon
    {
        public string ToCrypto { get; set; }
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
        public string FiatCurrency { get; set; }
        public SwapProvidersEnum[] Providers { get; set; }
    }

    public class SwapRateRequestJS : SwapRateCommon
    {
        public string ApiKey { get; set; }
    }

    public class SwapRateRequest : SwapRateCommon
    {
        public string FromCrypto { get; set; }
        public string FromNetwork { get; set; }
        public string ToNetwork { get; set; }
        public string ToCryptoNetwork { get; set; }

    }
}
