using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Satora.Models;

public class SatoraSettings
{
    [Key]
    public string StoreId { get; set; }


    [Display(Name = "Enabled in checkout")]
    public bool Enabled { get; set; }

}
