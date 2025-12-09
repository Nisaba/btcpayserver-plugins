namespace BTCPayServer.Plugins.B2PCentral.Models.Swaps
{
    public class B2PSwap
    {
        public SwapProvidersEnum Provider { get; set; }

        public string? FixedQuoteId { get; set; }
        public string? FloatQuoteId { get; set; }

        public float FromFixedAmount { get; set; }
        public float FromFloatAmount { get; set; }
        public float FixedRate { get; set; }
        public float FloatRate { get; set; }

        public float ToFixedAmount { get; set; }
        public float ToFloatAmount { get; set; }

        public float FromFiatFixedAmount { get; set; }
        public float FromFiatFloatAmount { get; set; }

        public float ToFiatFixedAmount { get; set; }
        public float ToFiatFloatAmount { get; set; }

        public string ValidUntil { get; set; } = string.Empty;
        public string? ReferalUrl { get; set; }

    }
}
