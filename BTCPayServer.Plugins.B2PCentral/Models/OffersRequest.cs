namespace BTCPayServer.Plugins.B2PCentral.Models
{
    /// <summary>
    /// Offers request model
    /// </summary>
    public struct OffersRequest
    {
        /// <summary>
        /// Fiat currency
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Filter on amount. If 0, return all offers 
        /// </summary>
        public uint Amount { get; set; }

        /// <summary>
        /// true = buy bitcoin offers, false = sell bitcoin offers
        /// </summary>
        public bool IsBuy {get; set;}

        /// <summary>
        /// Array of Providers used for research
        /// </summary>
        public ProvidersEnum[] Providers { get; set; }
    }
}
