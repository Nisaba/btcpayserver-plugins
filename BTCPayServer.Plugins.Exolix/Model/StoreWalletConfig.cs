namespace BTCPayServer.Plugins.Exolix.Model
{
    public class StoreWalletConfig
    {
        public string FiatCurrency { get; set; }
        public bool OnChainEnabled { get; set; }
        public decimal OnChainBalance { get; set; }
        public decimal OnChainFiatBalance { get; set; }
        public decimal Rate { get; set; }
    }
}
