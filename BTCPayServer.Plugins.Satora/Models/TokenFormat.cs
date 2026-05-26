using System.Globalization;

namespace BTCPayServer.Plugins.Satora.Models
{
    // Pure helpers for rendering swap fields in the back-office. The
    // SDK gives us amounts as decimal strings in the token's smallest
    // unit and addresses as plain strings; we want them formatted as
    // human-readable amounts and linked to block explorers.
    public static class TokenFormat
    {
        // Decimal places per token. Matches what the SDK / on-chain
        // contracts use. Anything we don't recognise falls back to 18
        // (standard ERC-20) — wrong, but at least visible.
        public static int DecimalsFor(string? tokenTypeName) => tokenTypeName switch
        {
            "Btc" => 8,
            "WbtcArbitrum" or "WbtcEthereum" or "WbtcPolygon" => 8,
            { } t when t.StartsWith("Usdc") => 6,
            { } t when t.StartsWith("Usdt") => 6,
            { } t when t.StartsWith("Eurc") => 6,
            _ => 18,
        };

        // Convert raw smallest-unit string -> human amount string in the
        // token's display precision. Returns the input unchanged if it
        // isn't a parseable integer (so e.g. error states render
        // gracefully).
        public static string FormatAmount(string? rawAmount, string? tokenTypeName)
        {
            if (string.IsNullOrEmpty(rawAmount)) return "";
            if (!decimal.TryParse(rawAmount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var raw))
                return rawAmount;
            var decimals = DecimalsFor(tokenTypeName);
            var divisor = (decimal)System.Math.Pow(10, decimals);
            return (raw / divisor).ToString($"F{decimals}", CultureInfo.InvariantCulture);
        }

        // Block-explorer URL for an address, dispatching on:
        //   - Arkade addresses (prefix `ark1`) -> arkade.space
        //   - EVM addresses (prefix `0x`) -> per-chain scanner, picked
        //     from the token suffix (Arbitrum / Ethereum / Polygon)
        // Returns null when we have no good link — the view falls back
        // to plain text.
        public static string? ExplorerUrl(string? address, string? tokenTypeName)
        {
            if (string.IsNullOrEmpty(address)) return null;
            if (address.StartsWith("ark1")) return $"https://arkade.space/address/{address}";
            if (address.StartsWith("0x"))
            {
                if (tokenTypeName?.EndsWith("Arbitrum") == true) return $"https://arbiscan.io/address/{address}";
                if (tokenTypeName?.EndsWith("Polygon") == true) return $"https://polygonscan.com/address/{address}";
                if (tokenTypeName?.EndsWith("Ethereum") == true) return $"https://etherscan.io/address/{address}";
            }
            return null;
        }
    }
}
