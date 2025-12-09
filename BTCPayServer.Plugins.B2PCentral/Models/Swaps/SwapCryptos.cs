using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BTCPayServer.Plugins.B2PCentral.Models.Swaps
{
    public enum CryptoCategory
    {
        Altcoin,
        StablecoinUSD,
        StablecoinEUR
    }

    public record CryptoInfo(string Name, CryptoCategory Category);

    public static class SwapCryptos
    {
        public static readonly IReadOnlyDictionary<string, CryptoInfo> AvailableCryptos = new ReadOnlyDictionary<string, CryptoInfo>(
            new Dictionary<string, CryptoInfo>
            {
            // Altcoins
            { "XMR", new CryptoInfo("Monero", CryptoCategory.Altcoin) },
            //{ "BTC-LIQ", new CryptoInfo("Liquid Bitcoin", CryptoCategory.Altcoin) },
            { "BCH", new CryptoInfo("Bitcoin Cash", CryptoCategory.Altcoin) },
            { "LTC", new CryptoInfo("Litecoin", CryptoCategory.Altcoin) },
            { "DOGE", new CryptoInfo("Dogecoin", CryptoCategory.Altcoin) },
            //{ "ETH", new CryptoInfo("Ethereum", CryptoCategory.Altcoin) },
            //{ "TRX", new CryptoInfo("Tron", CryptoCategory.Altcoin) },
            { "ADA", new CryptoInfo("Cardano", CryptoCategory.Altcoin) },
            //{ "SOL", new CryptoInfo("Solana", CryptoCategory.Altcoin) },
            
            // Stablecoins USD
            //{ "USDT-LIQ", new CryptoInfo("USDT Tether (Liquid)", CryptoCategory.StablecoinUSD) },
            //{ "USDT-ETH", new CryptoInfo("USDT Tether (Ethereum)", CryptoCategory.StablecoinUSD) },
            //{ "USDT-TRX", new CryptoInfo("USDT Tether (Tron)", CryptoCategory.StablecoinUSD) },
            //{ "USDT-BSC", new CryptoInfo("USDT Tether (Binance)", CryptoCategory.StablecoinUSD) },
            //{ "USDT-SOL", new CryptoInfo("USDT Tether (Solana)", CryptoCategory.StablecoinUSD) },
            //{ "USDT-MATIC", new CryptoInfo("USDT Tether (Polygon)", CryptoCategory.StablecoinUSD) },
            //{ "USDC-ETH", new CryptoInfo("USDC Circle (Ethereum)", CryptoCategory.StablecoinUSD) },
            //{ "USDC-BSC", new CryptoInfo("USDC Circle (Binance)", CryptoCategory.StablecoinUSD) },
            //{ "USDC-SOL", new CryptoInfo("USDC Circle (Solana)", CryptoCategory.StablecoinUSD) },
            //{ "USDC-MATIC", new CryptoInfo("USDC Circle (Polygon)", CryptoCategory.StablecoinUSD) },
            
            // Stablecoins EUR
            //{ "EURT-ETH", new CryptoInfo("EURT Tether (Ethereum)", CryptoCategory.StablecoinEUR) },
            //{ "EURI-ETH", new CryptoInfo("EURI Eurite (Ethereum)", CryptoCategory.StablecoinEUR) },
            //{ "EURI-BSC", new CryptoInfo("EURI Eurite (Binance)", CryptoCategory.StablecoinEUR) },
            //{ "DEURO-ETH", new CryptoInfo("DEURO Decentralized Euro (Ethereum)", CryptoCategory.StablecoinEUR) },

            });

        public static readonly Dictionary<CryptoCategory, string> CategoryLabels = new()
        {
            { CryptoCategory.Altcoin, "Altcoins" },
            { CryptoCategory.StablecoinUSD, "Stablecoins USD" },
            { CryptoCategory.StablecoinEUR, "Stablecoins EUR" }
        };

        private static readonly Dictionary<string, string> NetworkMap = new()
        {
            // Prefix-based mappings
            { "Tron", "Tron" },
            { "ETH", "Ethereum" },
            { "SOL", "Solana" },
            { "LIQ", "Liquid" },
            { "BSC", "Binance" },
            { "MATIC", "Polygon" },
            // Exact match mappings
            { "LTC", "Litecoin" },
            { "BCH", "Bitcoin Cash" },
            { "DOGE", "Doge" },
            { "ADA", "Cardano" },
        };

        public static string GetNetwork(string toCrypto)
        {
            // Try prefix-based matching first
            var prefixMatch = NetworkMap.FirstOrDefault(x => toCrypto.Contains(x.Key));
            if (!string.IsNullOrEmpty(prefixMatch.Key))
            {
                return prefixMatch.Value;
            }

            // Try exact matching
            var exactMatch = toCrypto.Split("-")[1];
            return NetworkMap.TryGetValue(exactMatch, out var network) ? network : string.Empty;
        }

    }
}
