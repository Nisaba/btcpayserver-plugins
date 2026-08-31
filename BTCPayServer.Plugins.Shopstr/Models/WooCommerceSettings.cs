using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Shopstr.Models
{
    [PrimaryKey(nameof(StoreId))]
    public class WooCommerceSettings
    {
        [Key]
        public string StoreId { get; set; }

        [Required]
        public string WooCommerceUrl { get; set; }

        [Required]
        public string ConsumerKey { get; set; }

        [Required]
        public string ConsumerSecret { get; set; }

        public string Location { get; set; }

        public bool FlashSales { get; set; }

        public ConditionEnum Condition { get; set; }

        public string Restrictions { get; set; }

        public bool IsConfigured => !string.IsNullOrEmpty(WooCommerceUrl)
                                    && !string.IsNullOrEmpty(ConsumerKey)
                                    && !string.IsNullOrEmpty(ConsumerSecret);
    }
}
