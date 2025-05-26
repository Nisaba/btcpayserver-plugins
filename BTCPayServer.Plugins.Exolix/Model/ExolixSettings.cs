using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Exolix.Model;

public class ExolixSettings
{
    [Key]
    public string StoreId { get; set; }

    public bool Enabled { get; set; }

    [Display(Name = "Accepted Altcoins")]
    public List<string> AcceptedCryptos { get; set; }

    [Display(Name = "Email Swap information to customer")]
    public bool IsEmailToCustomer { get; set; }

    [Display(Name = "Allow customer to specify a refund address if the swap fails")]
    public bool AllowRefundAddress { get; set; }

}
