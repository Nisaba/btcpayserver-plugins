using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Shopstr.Models.Shopstr
{
    public class ShopstrSettings
    {
        [Key]
        public string StoreId { get; set; }

        public string ShopStrShop { get; set; }
    }
}
