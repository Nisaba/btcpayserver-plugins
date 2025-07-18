namespace BTCPayServer.Plugins.Lendasat.Models
{
    public class StoreWalletConfig
    {
        public string FiatCurrency {  get; set; }
        public bool OnChainEnabled { get; set; }

        public decimal OnChainBalance { get; set; }
        public decimal OnChainFiatBalance { get; set; }

        public decimal Rate { get; set; }

    }
}
