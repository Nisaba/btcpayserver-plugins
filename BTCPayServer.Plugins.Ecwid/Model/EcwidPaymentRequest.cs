namespace BTCPayServer.Plugins.Ecwid.Model
{
    public record EcwidPaymentRequest
    {
        public string ClientSecret { get; set; }
        public string EncryptedData { get; set; }
        public string BTCPayStoreID { get; set; }

    }
}
