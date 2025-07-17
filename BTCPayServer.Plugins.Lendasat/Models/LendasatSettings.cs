using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Security.Cryptography;
using System.Text;

namespace BTCPayServer.Plugins.Lendasat.Models;

public class LendasatSettings
{

    [Key]
    public string StoreId { get; set; }

    [Display(Name = "Your Lendasat API Key")]
    public string APIKey { get; set; }

    [NotMapped]
    public bool isConfigured
    {
        get
        {
            return !string.IsNullOrEmpty(APIKey);
        }
    }


}
