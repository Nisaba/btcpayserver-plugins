namespace BTCPayServer.Plugins.B2PCentral.Models.Swaps
{
    public class SwapCreationResponse
    {
        public string SwapId { get; set; }
        public bool Success { get; set; }
        public string StatusMessage { get; set; }
        public string FollowUrl { get; set; }
        public string ProviderUrl { get; set; }
        public string FromAddress { get; set; }
        public string TransactionHash { get; set; }
    }

    public class SwapCheckoutCreationResponse : SwapCreationResponse
    {
        public SwapProvidersEnum Provider { get; set; }
        public string ProviderName { get; set; }

        public decimal FromAmount { get; set; }
    }
}
