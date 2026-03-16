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

    public enum CryptoCategory
    {
        Altcoin,
        StablecoinUSD,
        StablecoinEUR
    }

    public record CryptoInfo(string Name, CryptoCategory Category);

    public static readonly IReadOnlyDictionary<string, CryptoInfo> AvailableCryptos = new ReadOnlyDictionary<string, CryptoInfo>(
        new Dictionary<string, CryptoInfo>
        {
            // Altcoins
            { "XMR", new CryptoInfo("Monero", CryptoCategory.Altcoin) },
            { "LTC", new CryptoInfo("Litecoin", CryptoCategory.Altcoin) },
            { "DOGE", new CryptoInfo("Dogecoin", CryptoCategory.Altcoin) },
            { "ETH", new CryptoInfo("Ethereum", CryptoCategory.Altcoin) },
            { "TRX", new CryptoInfo("Tron", CryptoCategory.Altcoin) },
            { "POL", new CryptoInfo("Polygon", CryptoCategory.Altcoin) },
            { "BNB", new CryptoInfo("Binance Coin", CryptoCategory.Altcoin) },
            { "ADA", new CryptoInfo("Cardano", CryptoCategory.Altcoin) },
            { "SOL", new CryptoInfo("Solana", CryptoCategory.Altcoin) },
            
            // Stablecoins USD
            { "DAI", new CryptoInfo("DAI", CryptoCategory.StablecoinUSD) },
            { "USDT-ETH", new CryptoInfo("USDT Tether (Ethereum)", CryptoCategory.StablecoinUSD) },
            { "USDT-TRX", new CryptoInfo("USDT Tether (Tron)", CryptoCategory.StablecoinUSD) },
            { "USDT-BSC", new CryptoInfo("USDT Tether (Binance)", CryptoCategory.StablecoinUSD) },
            { "USDT-SOL", new CryptoInfo("USDT Tether (Solana)", CryptoCategory.StablecoinUSD) },
            { "USDT-NEAR", new CryptoInfo("USDT Tether (NEAR)", CryptoCategory.StablecoinUSD) },
            { "USDT-MATIC", new CryptoInfo("USDT Tether (Polygon)", CryptoCategory.StablecoinUSD) },
            { "USDT-TON", new CryptoInfo("USDT Tether (TON)", CryptoCategory.StablecoinUSD) },
            { "USDT-AVAXC", new CryptoInfo("USDT Tether (Avalanche)", CryptoCategory.StablecoinUSD) },
            { "USDC-ETH", new CryptoInfo("USDC Circle (Ethereum)", CryptoCategory.StablecoinUSD) },
            { "USDC-BSC", new CryptoInfo("USDC Circle (Binance)", CryptoCategory.StablecoinUSD) },
            { "USDC-SOL", new CryptoInfo("USDC Circle (Solana)", CryptoCategory.StablecoinUSD) },
            { "USDC-NEAR", new CryptoInfo("USDC Circle (NEAR)", CryptoCategory.StablecoinUSD) },
            { "USDC-MATIC", new CryptoInfo("USDC Circle (Polygon)", CryptoCategory.StablecoinUSD) },
            { "USDC-AVAXC", new CryptoInfo("USDC Circle (Avalanche)", CryptoCategory.StablecoinUSD) },
            
            // Stablecoins EUR
            { "EURT-ETH", new CryptoInfo("EURT Tether (Ethereum)", CryptoCategory.StablecoinEUR) },
            { "EURI-ETH", new CryptoInfo("EURI Eurite (Ethereum)", CryptoCategory.StablecoinEUR) },
            { "EURI-BSC", new CryptoInfo("EURI Eurite (Binance)", CryptoCategory.StablecoinEUR) },
            { "DEURO-ETH", new CryptoInfo("DEURO Decentralized Euro (Ethereum)", CryptoCategory.StablecoinEUR) },

        });

    [NotMapped]
    public static readonly Dictionary<CryptoCategory, string> CategoryLabels = new()
    {
        { CryptoCategory.Altcoin, "Altcoins" },
        { CryptoCategory.StablecoinUSD, "Stablecoins USD" },
        { CryptoCategory.StablecoinEUR, "Stablecoins EUR" }
    };
}
