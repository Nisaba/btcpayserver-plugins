using BTCPayServer.Plugins.B2PCentral.Models.Swaps;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.B2PCentral.Models;

public class B2PSettings : IValidatableObject
{
    [Key]
    public string StoreId { get; set; }

    [Display(Name = "B2P API key")]
    [Required(ErrorMessage = "This field is mandatory.")]
    public string ApiKey { get; set; }

    [Display(Name = "Enable auto-swaps for on-chain paid invoices")]
    public bool OnChainAutoSwapEnabled { get; set; }

    [Display(Name = "Enable auto-swaps for Lightning paid invoices")]
    public bool LightningAutoSwapEnabled { get; set; }

    [Display(Name = "Minimum on-chain balance for auto-swaps (sats)")]
    public int OnChainAutoSwapThreshold { get; set; }

    [Display(Name = "Minimum Lightning balance for auto-swaps (sats)")]
    public int LightningAutoSwapThreshold { get; set; }

    [Display(Name = "Percentage of on-chain balance to swap")]
    public int OnChainAutoSwapPercent { get; set; }

    [Display(Name = "Percentage of Lightning balance to swap")]
    public int LightningAutoSwapPercent { get; set; }

    [Display(Name = "Provider for on-chain swaps")]
    public SwapProvidersEnum OnChainAutoSwapProvider { get; set; }

    [Display(Name = "Provider for Lightning swaps")]
    public SwapProvidersEnum LightningAutoSwapProvider { get; set;  }

    [Display(Name = "Swap Destination Crypto")]
    public string OnChainAutoSwapCryptoTo { get; set; }

    [Display(Name = "Swap Destination Crypto")]
    public string LightningAutoSwapCryptoTo { get; set; }

    [Display(Name = "Swap Destination Address")]
    public string OnChainAutoSwapAddressTo { get; set; }

    [Display(Name = "Swap Destination Address")]
    public string LightningAutoSwapAddressTo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OnChainAutoSwapEnabled && string.IsNullOrWhiteSpace(OnChainAutoSwapAddressTo))
        {
            yield return new ValidationResult(
                "On-chain swap destination address is required when on-chain auto-swaps are enabled.",
                [nameof(OnChainAutoSwapAddressTo)]);
        }

        if (LightningAutoSwapEnabled && string.IsNullOrWhiteSpace(LightningAutoSwapAddressTo))
        {
            yield return new ValidationResult(
                "Lightning swap destination address is required when Lightning auto-swaps are enabled.",
                [nameof(LightningAutoSwapAddressTo)]);
        }
    }
}
