using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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

    [NotMapped]
    public bool isConfigured
    {
        get
        {
            return AcceptedCryptos.Any() && Enabled;
        }
    }

    public static readonly IReadOnlyDictionary<string, string> AvailableCryptos = new ReadOnlyDictionary<string, string>(
        new Dictionary<string, string>
        {
            { "XMR", "Monero" },
            { "LTC", "Litecoin" },
            { "DOGE", "Dogecoin" },
            { "ETH", "Ethereum" },
            { "TRX", "Tron" },
            { "POL", "Polygon" },
            { "BNB", "Binance Coin" },
            { "ADA", "Cardano" },
            { "SOL", "Solana" },
            { "DAI", "DAI" },
            { "USDT-ETH", "USDT Tether (Ethereum)" },
            { "USDT-TRX", "USDT Tether (Tron)" },
            { "USDT-BSC", "USDT Tether (Binance)" },
            { "USDT-SOL", "USDT Tether (Solana)" },
            { "USDT-NEAR", "USDT Tether (NEAR)" },
            { "USDT-MATIC", "USDT Tether (Polygon)" },
            { "USDT-TON", "USDT Tether (TON)" },
            { "USDT-AVAXC", "USDT Tether (Avalanche)" },
            { "USDC-ETH", "USDC Circle (Ethereum)" },
            { "USDC-BSC", "USDC Circle (Binance)" },
            { "USDC-SOL", "USDC Circle (Solana)" },
            { "USDC-NEAR", "USDC Circle (NEAR)" },
            { "USDC-MATIC", "USDC Circle (Polygon)" },
            { "USDC-AVAXC", "USDC Circle (Avalanche)" }
        });
}
