using BTCPayServer.Plugins.B2PCentral.Models.Swaps;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.B2PCentral.Models
{
    [PrimaryKey(nameof(StoreId), nameof(SwapId))]
    public class B2PStoreSwap
    {
        [Key]
        public string StoreId { get; set; }
        [Key]
        public string SwapId { get; set; }
        public DateTime DateT { get; set; }
        public SwapProvidersEnum Provider { get; set; }
        public string ProviderUrl { get; set; }
        public string FollowUrl { get; set; }
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
        public string ToCrypto { get; set; }
        public string ToNetwork { get; set; }
        public string BTCPayPullPaymentId { get; set; }
        public string BTCPayPayoutId { get; set; }
    }
}
