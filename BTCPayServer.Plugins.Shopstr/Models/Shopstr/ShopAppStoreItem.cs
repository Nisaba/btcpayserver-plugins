using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Shopstr.Models.Shopstr
{
    public class ShopAppStoreItem
    {
        [Key]
        public string ItemId { get; set; }

        [Required]
        public string StoreId { get; set; }

        [Required]
        public string AppId { get; set; }

        [Required]
        public string Hash { get; set; }

    }
}
