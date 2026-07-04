namespace BTCPayServer.Plugins.B2PCentral.Models
{
    public class ProviderInfo
    {
        public int NumProvider { get; set; }
        public string ReliabilityDescription { get; set; }
        public string ToSDescription { get; set; }
        public string PrivacyDescription { get; set; }
        public string KYCDescription { get; set; }
        public string KYCColor { get; set; }
        public string[] TermsConditionsUrls { get; set; }

        public bool IsLightning { get; set; }
        public bool IsLiquid { get; set; }

    }

}
