using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;

namespace BTCPayServer.Plugins.MtPelerin.Model;

public class MtPelerinSettings
{
    [Key]
    public string StoreId { get; set; }

    [Display(Name = "Mt Pelerin API Key")]
    [Required(ErrorMessage = "API Key is mandatory.")]
    public string ApiKey { get; set; }

    [Display(Name = "Display language")]
    public string Lang { get; set; }

    [Display(Name = "Your phone")]
    public string Phone { get; set; }

}
