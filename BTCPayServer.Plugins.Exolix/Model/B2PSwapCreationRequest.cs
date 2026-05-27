
using System.ComponentModel;

namespace BTCPayServer.Plugins.Exolix.Model
{
    public class B2PSwapCreationRequest
    {
        public SwapProvidersEnum Provider { get; set; } = SwapProvidersEnum.Exolix;
        public string QuoteID { get; set; } = string.Empty;
        public string ToCrypto { get; set; }
        public float FromAmount { get; set; }
        public float ToAmount { get; set; }
        public string ToAddress { get; set; }
        public string FromRefundAddress { get; set; } = string.Empty;
        public bool IsFixed { get; set; } = true;
        public string NotificationEmail { get; set; } = string.Empty;
        public string FromCrypto { get; set; }
        public string FromNetwork { get; set; }
        public string ToNetwork { get; set; }
        public string NotificationNpub { get; set; } = string.Empty;

    }

    public enum SwapProvidersEnum
    {

        [Description("Exolix")]
        Exolix = 14,

    }

}
