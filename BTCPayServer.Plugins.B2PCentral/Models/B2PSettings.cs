using BTCPayServer.Plugins.B2PCentral.Models.P2P;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace BTCPayServer.Plugins.B2PCentral.Models;

public class B2PSettings
{
    [Key]
    public string StoreId { get; set; }

    [Display(Name = "B2P API key")]
    [Required(ErrorMessage = "This field is mandatory.")]
    public string ApiKey { get; set; }

    public string ProvidersString { get; set; }

    [NotMapped]
    public List<ProvidersEnum> Providers
    {
        get
        {
            return string.IsNullOrEmpty(ProvidersString) ? new  List<ProvidersEnum>() : ProvidersString.Split(",").ToList().Select(s => (ProvidersEnum)Enum.Parse(typeof(ProvidersEnum), s)).ToList();
        }
        set
        {
            ProvidersString = string.Join(",", value.ToArray());
        }
    }
}
