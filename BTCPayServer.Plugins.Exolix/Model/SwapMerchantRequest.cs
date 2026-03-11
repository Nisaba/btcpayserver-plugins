namespace BTCPayServer.Plugins.Exolix.Model
{
    public class SwapMerchantRequest
    {
        public string ToCrypto { get; set; }

        public string ToAddress { get; set; }

        public float BtcAmount { get; set; }

        public float ToAmount { get; set; }

    }
}
