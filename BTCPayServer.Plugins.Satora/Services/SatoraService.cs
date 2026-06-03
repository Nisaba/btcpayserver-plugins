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
                    (Stablecoins.USDC, Blockchains.Polygon)   => new TokenId.UsdcPolygon(),
                    (Stablecoins.USDC, Blockchains.Arbitrum)  => new TokenId.UsdcArbitrum(),
                    (Stablecoins.USDC, Blockchains.Ethereum)  => new TokenId.UsdcEthereum(),
                    (Stablecoins.USDT, Blockchains.Polygon)   => new TokenId.UsdtPolygon(),
                    (Stablecoins.USDT, Blockchains.Ethereum)  => new TokenId.UsdtEthereum(),
                    (Stablecoins.USDT0, Blockchains.Arbitrum) => new TokenId.Usdt0Arbitrum(),
                    (Stablecoins.WBTC, Blockchains.Polygon)   => new TokenId.WbtcPolygon(),
                    (Stablecoins.WBTC, Blockchains.Arbitrum)  => new TokenId.WbtcArbitrum(),
                    (Stablecoins.WBTC, Blockchains.Ethereum)  => new TokenId.WbtcEthereum(),
                    _ => new TokenId.Other($"{req.CryptoFrom}-{req.NetworkFrom}")
                };

                QuoteAmount quoteAmount = new QuoteAmount.Target((ulong)(req.BtcAmount * 100000000));

                ChainId chainTo = req.BtcNetwork switch
                {
                    "BTC"       => new ChainId.Bitcoin(),
                    "LIGHTNING" => new ChainId.Lightning(),
                    "ARKADE"    => new ChainId.Arkade(),
                    _           => new ChainId.Other(req.BtcNetwork)
                };

                Address addressTo = req.BtcNetwork switch
                {
                    "BTC"       => new Address.Bitcoin(req.BtcDestination),
                    "LIGHTNING" => new Address.Lightning(req.BtcDestination),
                    "ARKADE"    => new Address.Arkade(req.BtcDestination),
                    _           => new Address()
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
                    Stablecoins.EURC  => 6,
                    Stablecoins.USDC  => 6,
                    Stablecoins.USDT  => 6,
                    Stablecoins.USDT0 => 6,
                    Stablecoins.WBTC  => 8,
                    _ => 18
                };

                var rawAmount = double.Parse(apiSwap.DepositAmount, CultureInfo.InvariantCulture);

                var swap = new SwapResponse
                {
                    SwapId        = apiSwap.Id,
                    FromAddress   = sDepositAddress,
                    FromAmount    = (float)(rawAmount / Math.Pow(10, decimals)),
                    Success       = true,
                    StatusMessage = "Swap created successfully"
                };
                logger.LogInformation("Satora Swap Created : {SwapId} {FromAmount} {From} -> {To}",
                    swap.SwapId, swap.FromAmount, $"{req.CryptoFrom}-{req.NetworkFrom}", req.BtcNetwork);
                return swap;
            }
            catch (Exception ex)
            {
                logger.LogError("SatoraPlugin.CreateSwap(): {Message} - {From} -> {To}",
                    ex.Message, $"{req.CryptoFrom}-{req.NetworkFrom}", req.BtcNetwork);
                throw;
            }
        }

        public async Task<string> GetSwapInfoAsync(string id, string seedPhrase)
        {
            try
            {
                using var client = new global::Satora.Sdk.Client(seedPhrase);
                var swap = await client.GetSwapAsync(id);
                return swap.Status.GetType().Name;
            }
            catch (Exception ex)
            {
                logger.LogError("SatoraPlugin.GetSwapInfo(): {Message} - {Id}", ex.Message, id);
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
                logger.LogError("SatoraPlugin.GetSwapAsync(): {Message} - {Id}", ex.Message, id);
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
                logger.LogError("SatoraPlugin.FundSwapAsync(): {Message} - {SwapId}", ex.Message, swapId);
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
                logger.LogError("SatoraPlugin.CheckDepositAsync(): {Message} - {SwapId}", ex.Message, swapId);
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
                logger.LogError("SatoraPlugin.ClaimAsync(): {Message} - {SwapId} -> {Dest}", ex.Message, swapId, destination);
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
                logger.LogError("SatoraPlugin.GetArkadeAddressAsync(): {Message}", ex.Message);
                throw;
            }
        }

        public async Task<string> SweepToAddressAsync(string destinationArkadeAddress, ulong amountSats, string seedPhrase)
        {
            try
            {
                using var client = new global::Satora.Sdk.Client(seedPhrase);

                var sendTxId = await client.SendArkadeAsync(destinationArkadeAddress, amountSats);

                logger.LogInformation("SatoraPlugin.SweepToAddressAsync(): swept {Sats} sats to {Dest}, ark_txid={Txid}",
                    amountSats, destinationArkadeAddress, sendTxId);

                return sendTxId;
            }
            catch (Exception ex)
            {
                logger.LogError("SatoraPlugin.SweepToAddressAsync(): {Message} - {Sats} sats -> {Dest}",
                    ex.Message, amountSats, destinationArkadeAddress);
                throw;
            }
        }
    }
}
