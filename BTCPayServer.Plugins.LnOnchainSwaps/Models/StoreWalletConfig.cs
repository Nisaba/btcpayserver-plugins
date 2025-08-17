namespace BTCPayServer.Plugins.LnOnchainSwaps.Models
{
    public class StoreWalletConfig
    {
        public string FiatCurrency {  get; set; }
        public bool OnChainEnabled { get; set; }
        public bool OffChainEnabled { get; set;}

        public decimal OnChainBalance { get; set; }
        public decimal OnChainFiatBalance { get; set; }
        public decimal OffChainBalance { get; set; }
        public decimal OffChainFiatBalance { get; set; }

        public decimal Rate { get; set; }

        public bool OnChainAvailable
        {
            get { return OnChainEnabled && OnChainBalance > 0; }
        }

        public bool OffChainAvailable
        {
            get { return OffChainEnabled && OffChainBalance > 0; }
        }


    }
}
