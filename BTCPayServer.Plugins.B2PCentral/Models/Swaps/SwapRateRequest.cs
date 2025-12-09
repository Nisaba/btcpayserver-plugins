namespace BTCPayServer.Plugins.B2PCentral.Models.Swaps
{
    public class SwapRateRequestJS
    {
        public string ToCrypto { get; set; }
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
        public string FiatCurrency { get; set; }
        public SwapProvidersEnum[] Providers { get; set; }
        public string ApiKey { get; set; }
    }

    public class SwapRateRequest: SwapRateRequestJS
    {
        public string FromCrypto { get; set; }
        public string FromNetwork { get; set; }
        public string ToNetwork { get; set; }
    }
}
