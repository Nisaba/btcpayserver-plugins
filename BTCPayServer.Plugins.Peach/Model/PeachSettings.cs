using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTCPayServer.Plugins.Peach.Model;

public class PeachSettings
{


    [Key]
    public string StoreId { get; set; }

    [Display(Name = "Your Peach Public Key")]
    public string PublicKey { get; set; }

    public string PrivateKey { get; set; }

    public bool IsRegistered { get; set; }


    [NotMapped]
    public bool isConfigured
    {
        get
        {
            return !string.IsNullOrEmpty(PublicKey) && !string.IsNullOrEmpty(PrivateKey);
        }
    }

}
