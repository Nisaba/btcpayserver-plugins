using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Ecwid.Model;

public class EcwidSettings
{
    [Key]
    public string StoreId { get; set; }

    [Display(Name = "Ecwid Client Secret")]
    [Required(ErrorMessage = "This field is mandatory.")]
    public string ClientSecret { get; set; }

}
