using BTCPayServer.Plugins.Satora.Models;
using System.Globalization;
using uniffi.satora_sdk_ffi;

namespace BTCPayServer.Plugins.Satora.Services
{
    public class SatoraService(ILogger<SatoraService> logger)
    {
       public async Task<SwapResponse> CreateSwapAsync(SwapRequest req, string seedPhrase)
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
                    (Stablecoins.USDT, Blockchains.Ethereum) => new TokenId.UsdtEthereum(),

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

                Address addressTo = req.BtcNetwork switch
                {
                    "BTC" => new Address.Bitcoin(req.BtcDestination),
                    "LIGHTNING" => new Address.Lightning(req.BtcDestination),
                    "ARKADE" => new Address.Arkade(req.BtcDestination),
                    _ => new Address()
                };

                using var client = new global::Satora.Sdk.Client(seedPhrase);
                var apiSwap = await client.CreateSwapAsync(chainFrom, tokenFrom, chainTo, new TokenId.Btc(), quoteAmount, addressTo, true);
                
                string? sDepositAddress = apiSwap.Funding switch
                {
                    SwapFunding.Gasless gasless => gasless.depositAddress,
                    _ => null
                } ?? throw new Exception("Failed to retrieve deposit address");

                int decimals = req.CryptoFrom switch
                {
                    Stablecoins.EURC => 6,
                    Stablecoins.USDC => 6,
                    Stablecoins.USDT => 6,
                    Stablecoins.USDT0 => 6,
                    Stablecoins.WBTC => 8,
                    _ => 18 
                };
                var divisor = Math.Pow(10, decimals);

                var rawAmount = double.Parse(apiSwap.DepositAmount, CultureInfo.InvariantCulture);

                var swap = new SwapResponse
                {
                    SwapId = apiSwap.Id,
                    FromAddress = sDepositAddress,
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

        public async Task<string> GetSwapInfoAsync(string id, string seedPhrase)
        {
            try
            {
                using var client = new global::Satora.Sdk.Client(seedPhrase);
                var swap = await client.GetSwapAsync(id);
                // Use the variant type name (e.g. "Pending") rather than
                // the record's default ToString ("Pending { }") so the
                // local-cache column and the details page stay readable
                // and consistent with what ContinueSwapAsync writes.
                return swap.Status.GetType().Name;
            }
            catch (Exception ex)
            {
                logger.LogError($"SatoraPlugin.GetSwapInfo(): {ex.Message} - {id}");
                throw;
            }
        }

        public async Task<global::Satora.Sdk.SwapDetails> GetSwapAsync(string id, string seedPhrase)
        {
            try
            {
                using var client = new global::Satora.Sdk.Client(seedPhrase);
                return await client.GetSwapAsync(id);
            }
            catch (Exception ex)
            {
                logger.LogError($"SatoraPlugin.GetSwapAsync(): {ex.Message} - {id}");
                throw;
            }
        }

        public async Task<global::Satora.Sdk.FundReceipt> FundSwapAsync(string swapId, string seedPhrase)
        {
            try
            {
                using var client = new global::Satora.Sdk.Client(seedPhrase);
                return await client.FundSwapAsync(swapId);
            }
            catch (Exception ex)
            {
                logger.LogError($"SatoraPlugin.FundSwapAsync(): {ex.Message} - {swapId}");
                throw;
            }
        }

        public async Task<global::Satora.Sdk.DepositStatus> CheckDepositAsync(string swapId, string seedPhrase)
        {
            try
            {
                using var client = new global::Satora.Sdk.Client(seedPhrase);
                return await client.CheckDepositAsync(swapId);
            }
            catch (Exception ex)
            {
                logger.LogError($"SatoraPlugin.CheckDepositAsync(): {ex.Message} - {swapId}");
                throw;
            }
        }

        public async Task<global::Satora.Sdk.ClaimReceipt> ClaimAsync(string swapId, string destination, string seedPhrase)
        {
            try
            {
                using var client = new global::Satora.Sdk.Client(seedPhrase);
                return await client.ClaimAsync(swapId, destination);
            }
            catch (Exception ex)
            {
                logger.LogError($"SatoraPlugin.ClaimAsync(): {ex.Message} - {swapId} -> {destination}");
                throw;
            }
        }

        public async Task<string> GetArkadeAddressAsync(string seedPhrase)
        {
            try
            {
                using var client = new global::Satora.Sdk.Client(seedPhrase);
                return await client.GetArkadeAddressAsync();
            }
            catch (Exception ex)
            {
                logger.LogError($"SatoraPlugin.GetArkadeAddressAsync(): {ex.Message}");
                throw;
            }
        }
    }
}
