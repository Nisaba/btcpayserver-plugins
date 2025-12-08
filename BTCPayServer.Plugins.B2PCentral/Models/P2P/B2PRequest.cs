namespace BTCPayServer.Plugins.B2PCentral.Models.P2P
{
    public class B2PRequest
    {
        public decimal Rate { get; set; }
        public string ApiKey { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Amount { get; set; }
        public ProvidersEnum[] Providers { get; set; }


    }
}
