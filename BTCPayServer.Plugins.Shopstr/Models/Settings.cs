using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Shopstr.Models
{
    public class Settings
    {
        [Key]
        public string StoreId { get; set; }
    }
}
