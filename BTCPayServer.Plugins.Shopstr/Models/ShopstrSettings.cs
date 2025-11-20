using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Shopstr.Models
{
    public class ShopstrSettings
    {
        [Key]
        public string StoreId { get; set; }

        public string ShopStrMarketplace { get; set; }
    }
}
