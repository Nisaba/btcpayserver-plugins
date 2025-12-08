using Fido2NetLib;
using System.ComponentModel;

namespace BTCPayServer.Plugins.B2PCentral.Models.Swaps
{
    public enum SwapProvidersEnum
    {
        NONE = 0,
        
        [Description("Exchange.net")]
        EXCH_NET = 1,

        [Description("Fixed Float")]
        FIXED_FLOAT = 2,

        [Description("SideShift")]
        SIDE_SHIFT = 3,

        [Description("Swaponix")]
        SWAPONIX = 4,

        [Description("Trocador")]
        TROCADOR = 5,

        [Description("StealthEx")]
        STEALTH_EX = 6,

        [Description("SimpleSwap")]
        SIMPLE_SWAP = 7,

        [Description("ChangeNow")]
        CHANGE_NOW = 8,

        [Description("BitcoinVN")]
        BITCOIN_VN = 9,

        [Description("ChangeHero")]
        CHANGE_HERO = 10,

        [Description("Godex")]
        GODEX = 11,

        [Description("SwapSpace")]
        SWAP_SPACE = 12,

        [Description("Changelly")]
        CHANGELLY = 13,

        [Description("Exolix")]
        EXOLIX = 14,

        [Description("EasyBit")]
        EASY_BIT = 15,

        [Description("AvanChange")]
        AVAN_CHANGE = 16,

        [Description("PegasusSwap")]
        PEGASUS_SWAP = 17,

        [Description("MajesticBank")]
        MAJESTIC_BANK = 18,
    }

    public static class SwapProviders
    {
        public static readonly SwapProvidersEnum[] InactiveProviders = [
            SwapProvidersEnum.NONE,
            SwapProvidersEnum.EXCH_NET,
            SwapProvidersEnum.TROCADOR,
            SwapProvidersEnum.FIXED_FLOAT,
            SwapProvidersEnum.CHANGE_NOW,
            SwapProvidersEnum.BITCOIN_VN,
            SwapProvidersEnum.SWAP_SPACE,
            SwapProvidersEnum.AVAN_CHANGE,
            SwapProvidersEnum.MAJESTIC_BANK
        ];

        public static readonly SwapProvidersEnum[] ActiveProviders = [
            SwapProvidersEnum.SIDE_SHIFT,
            SwapProvidersEnum.SWAPONIX,
            SwapProvidersEnum.STEALTH_EX,
            SwapProvidersEnum.SIMPLE_SWAP,
            SwapProvidersEnum.CHANGE_HERO,
            SwapProvidersEnum.GODEX,
            SwapProvidersEnum.CHANGELLY,
            SwapProvidersEnum.EXOLIX,
            SwapProvidersEnum.EASY_BIT,
            SwapProvidersEnum.PEGASUS_SWAP
        ];

        public static readonly SwapProvidersEnum[] KYCProviders = [
            SwapProvidersEnum.NONE,
            SwapProvidersEnum.CHANGELLY,
            SwapProvidersEnum.SIMPLE_SWAP,
            SwapProvidersEnum.CHANGE_HERO,
            SwapProvidersEnum.STEALTH_EX,
            SwapProvidersEnum.EASY_BIT,
        ];
    }
}
