namespace BTCPayServer.Plugins.Ecwid.Model
{
    public struct EcwidWebhookModel
    {
        public string TransactionId { get; set; }
        public string StoreId { get; set; }
        public string Token { get; set; }
        public string PaymentStatus { get; set; }


    }
}
