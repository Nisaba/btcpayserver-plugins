using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using YamlDotNet.Core.Tokens;

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

    [Display(Name = "Your phone (numbers only)")]
    public string Phone { get; set; }

    [NotMapped]
    public int PhoneInt
    {
        get
        {
            if (string.IsNullOrEmpty(Phone))
                return 0;
            var sPhone = Phone.StartsWith("00") ? Phone[2..] : Phone;
            if (int.TryParse(sPhone, out int phoneInt))
                return phoneInt;
            return 0;
        }
    }

}
