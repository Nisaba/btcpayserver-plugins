namespace BTCPayServer.Plugins.Ecwid.Model
{
    public struct EcwidCallbackModel
    {
        public string TransactionId { get; set; }
        public string StoreId { get; set; }
        public string Token { get; set; }
        public string PaymentStatus { get; set; }


    }
}
