using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Ecwid.Data;

public class EcwidSettings
{
    [Key]
    public string StoreId { get; set; }

    [Display(Name = "Webhook Secret")]
    //[Required(ErrorMessage = "This field is mandatory.")]
    public string WebhookSecret { get; set; }

    [Display(Name = "Ecwid Client Secret")]
    [Required(ErrorMessage = "This field is mandatory.")]
    public string ClientSecret { get; set; }

}
