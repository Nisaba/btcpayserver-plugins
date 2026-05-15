using BTCPayServer.Plugins.Satora.Models;
using Lendaswap.Sdk;
using System.Globalization;
using uniffi.lendaswap_sdk_ffi;

namespace BTCPayServer.Plugins.Satora.Services
{
    public class SatoraService(ILogger<SatoraService> logger, Lendaswap.Sdk.Client client)
    {
       public async Task<SwapResponse> CreateSwapAsync(SwapRequest req)
       {
            try
            {
                ChainId chainFrom = req.NetworkFrom switch
                {
                    Blockchains.Arbitrum => new ChainId.Arbitrum(),
                    Blockchains.Ethereum => new ChainId.Ethereum(),
                    Blockchains.Polygon => new ChainId.Polygon(),
                    _ => new ChainId.Other(req.NetworkFrom.ToString())
                };

                TokenId tokenFrom = (req.CryptoFrom, req.NetworkFrom) switch
                {
                    // USDC
                    (Stablecoins.USDC, Blockchains.Polygon) => new TokenId.UsdcPolygon(),
                    (Stablecoins.USDC, Blockchains.Arbitrum) => new TokenId.UsdcArbitrum(),
                    (Stablecoins.USDC, Blockchains.Ethereum) => new TokenId.UsdcEthereum(),

                    // USDT
                    (Stablecoins.USDT, Blockchains.Polygon) => new TokenId.UsdtPolygon(),
                    (Stablecoins.USDT, _) => new TokenId.UsdtEthereum(),

                    // USDT0
                    (Stablecoins.USDT0, Blockchains.Arbitrum) => new TokenId.Usdt0Arbitrum(),

                    // WBTC
                    (Stablecoins.WBTC, Blockchains.Polygon) => new TokenId.WbtcPolygon(),
                    (Stablecoins.WBTC, Blockchains.Arbitrum) => new TokenId.WbtcArbitrum(),
                    (Stablecoins.WBTC, Blockchains.Ethereum) => new TokenId.WbtcEthereum(),

                    // Fallback
                    _ => new TokenId.Other($"{req.CryptoFrom}-{req.NetworkFrom}")
                };

                QuoteAmount quoteAmount = new QuoteAmount.Target((ulong)(req.BtcAmount * 100000000));

                ChainId chainTo = req.BtcNetwork switch
                {
                    "BTC" => new ChainId.Bitcoin(),
                    "LIGHTNING" => new ChainId.Lightning(),
                    "ARKADE" => new ChainId.Arkade(),
                    _ => new ChainId.Other(req.BtcNetwork)
                };

                Quote quote = await client.GetQuoteAsync(chainFrom, tokenFrom, chainTo, new TokenId.Btc(), quoteAmount);
                int decimals = req.CryptoFrom switch
                {
                    Stablecoins.EURC => 6,
                    Stablecoins.USDC => 6,
                    Stablecoins.USDT => 6,
                    Stablecoins.USDT0 => 6,
                    Stablecoins.WBTC => 8,
                    _ => 18 
                };

                var rawAmount = double.Parse(quote.SourceAmount, CultureInfo.InvariantCulture);
                var divisor = Math.Pow(10, decimals);

                var swap = new SwapResponse
                {
                    SwapId = "SATORA#" + Random.Shared.Next(100000, 999999).ToString(),
                    FromAddress = "FromAddressPlaceholder",
                    FromAmount = (float)(rawAmount / divisor),
                    Success = true,
                    StatusMessage = "Swap created successfully"
                };
                logger.LogInformation($"Satora Swap Created : {swap.SwapId} {swap.FromAmount} {req.CryptoFrom}-{req.NetworkFrom} -> {req.BtcNetwork}");
                return swap;
            }
            catch (Exception ex)
            {
                logger.LogError($"SatoraPlugin.CreateSwap(): {ex.Message} - {req.CryptoFrom}-{req.NetworkFrom} -> {req.BtcNetwork}");
                 throw;
            }

        }
    }
}
