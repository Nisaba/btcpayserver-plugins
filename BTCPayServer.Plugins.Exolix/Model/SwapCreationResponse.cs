namespace BTCPayServer.Plugins.Exolix.Model
{
    public class SwapCreationResponse
    {
        public bool Success { get; set; }
        public string SwapId { get; set; }
        public string StatusMessage { get; set; }
        public string FromAddress { get; set; }
        public float FromAmount { get; set; }
        public float ToAmount { get; set; }

    }
}
