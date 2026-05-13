namespace BTCPayServer.Plugins.Satora.Models
{
    public class SwapResponse
    {
        public bool Success { get; set; }
        public string SwapId { get; set; }
        public string StatusMessage { get; set; }
        public string FromAddress { get; set; }
        public float FromAmount { get; set; }

    }
}
