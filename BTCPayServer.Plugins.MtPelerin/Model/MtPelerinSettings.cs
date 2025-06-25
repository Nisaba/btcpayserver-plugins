using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using YamlDotNet.Core.Tokens;

namespace BTCPayServer.Plugins.MtPelerin.Model;

public class MtPelerinSettings
{

    public const string ApiKey = "746407c5-5986-44bf-8c7f-b36c6f180207";

    //public const string BtcDestAdress = "bcrt1qpzfyktpawhcy66ctqpujdhfxsm8atjqzezq9p4";
    public const string BtcDestAdress = "3LgdKdB9x42m4ujae78NcwUXjYW3z45KrX";

    [Key]
    public string StoreId { get; set; }

    [Display(Name = "Display language")]
    public string Lang { get; set; }

    [Required]
    [Display(Name = "Your phone (numbers only)")]
    public string Phone { get; set; }

    [NotMapped]
    public ulong PhoneInt
    {
        get
        {
            if (string.IsNullOrEmpty(Phone))
                return 0;
            var sPhone = Phone.StartsWith("00") ? Phone[2..] : Phone.Replace("+", "");
            if (ulong.TryParse(sPhone, out ulong phoneInt))
                return phoneInt;
            return 0;
        }
    }

}
