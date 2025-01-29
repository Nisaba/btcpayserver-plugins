using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace BTCPayServer.Plugins.Ecwid.Data;

public class EcwidSettings
{
    [Key]
    public string StoreId { get; set; }

    [Display(Name = "API key")]
    [Required(ErrorMessage = "This field is mandatory.")]
    public string ApiKey { get; set; }

    public string ProvidersString { get; set; }

}
