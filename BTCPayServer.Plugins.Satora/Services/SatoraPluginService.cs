using BTCPayServer.Plugins.Satora.Data;
using BTCPayServer.Plugins.Satora.Models;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using uniffi.satora_sdk_ffi;

namespace BTCPayServer.Plugins.Satora.Services
{
    public class SatoraPluginService(SatoraPluginDbContextFactory pluginDbContextFactory, ILogger<SatoraPluginService> logger, SatoraService satoraService)
    {

        public async Task<SatoraSettings> GetStoreSettings(string storeId)
        {
            try
            {
                await using var _context = pluginDbContextFactory.CreateContext();
                var settings = await _context.SatoraSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                if (settings == null)
                {
                    settings = new SatoraSettings
                    {
                        StoreId = storeId,
                        Enabled = false,
                        Seed = new Mnemonic(Wordlist.English, WordCount.Twelve).ToString()
                    };
                }
                return settings;
            }
            catch (Exception e)
            {
                logger.LogError(e, "SatoraPlugin:GetStoreSettings()");
                throw;
            }
        }

        public async Task<SatoraModel> GetStoreData(string storeId)
        {
            try
            {
                await using var _context = pluginDbContextFactory.CreateContext();
                var settings = await _context.SatoraSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                if (settings == null)
                {
                    settings = new SatoraSettings
                    {
                        StoreId = storeId,
                        Enabled = false
                    };
                }

                var txs = await _context.SatoraTransactions
                    .Where(a => a.StoreId == storeId)
                    .OrderByDescending(a => a.DateT)
                    .ToListAsync();

                return new SatoraModel {
                    Settings = settings,
                    Transactions = txs
                };

            }
            catch (Exception e)
            {
                logger.LogError(e, "SatoraPlugin:GetStoreData()");
                throw;
            }
        }

        public async Task UpdateSettings(SatoraSettings settings)
        {
            try
            {
                if (settings.Seed == null)
                {
                    settings.Seed = new Mnemonic(Wordlist.English, WordCount.Twelve).ToString();
                }

                await using var _context = pluginDbContextFactory.CreateContext();
                var dbSettings = await _context.SatoraSettings.FirstOrDefaultAsync(a => a.StoreId == settings.StoreId);
                if (dbSettings == null)
                {
                    _context.SatoraSettings.Add(settings);
                }
                else
                {
                    dbSettings.Enabled = settings.Enabled;
                    dbSettings.Seed = settings.Seed;
                    _context.SatoraSettings.Update(dbSettings);
                }

                await _context.SaveChangesAsync();
                return;

            }
            catch (Exception e)
            {
                logger.LogError(e, "SatoraPlugin:UpdateSettings()");
                throw;
            }
        }

        public async Task AddStoreTransaction(SatoraTx tx)
        {
            try
            {
                await using var _context = pluginDbContextFactory.CreateContext();
                await _context.SatoraTransactions.AddAsync(tx);
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, "SatoraPlugin:AddStoreTransaction()");
                throw;
            }
        }

        public async Task<string?> GetSeedPhraseBySwapId(string swapId)
        {
            try
            {
                await using var _context = pluginDbContextFactory.CreateContext();
                var dbSwap = await _context.SatoraTransactions.FirstOrDefaultAsync(s => s.TxID == swapId);
                var settings = await _context.SatoraSettings.FirstOrDefaultAsync(s => s.StoreId == dbSwap.StoreId);
                return settings?.Seed;
            }
            catch (Exception e)
            {
                logger.LogError(e, "SatoraPlugin:GetSeedPhraseBySwapId()");
                throw;
            }
        }


        public async Task<string> DoGetSwapStatus(string swapId)
        {
            try
            {
                using var _context = pluginDbContextFactory.CreateContext();
                var dbSwap = await _context.SatoraTransactions.FirstOrDefaultAsync(s => s.TxID == swapId);

                var storeSettings = await _context.SatoraSettings.FirstOrDefaultAsync(s => s.StoreId == dbSwap.StoreId);
                var status = await satoraService.GetSwapInfoAsync(swapId, storeSettings.Seed);
                if (dbSwap.Status != status)
                {
                    dbSwap.Status = status;
                    _context.SatoraTransactions.Update(dbSwap);
                    await _context.SaveChangesAsync();
                }
                return status;
            }
            catch (Exception e)
            {
                logger.LogError(e, "SatoraPlugin:DoGetSwapStatus()");
                throw;
            }
        }

        // Build the swap details view model — local DB row plus a fresh
        // pull from the Satora backend. Both lookups are best-effort: the
        // page is useful even when one fails, so errors are returned in
        // the model rather than thrown.
        public async Task<SwapDetailsModel> GetSwapDetailsAsync(string storeId, string swapId)
        {
            var model = new SwapDetailsModel { StoreId = storeId };

            await using var _context = pluginDbContextFactory.CreateContext();
            var storeSettings = await _context.SatoraSettings.FirstOrDefaultAsync(s => s.StoreId == storeId);
            model.LocalTx = await _context.SatoraTransactions
                .FirstOrDefaultAsync(s => s.TxID == swapId && s.StoreId == storeId);

            try
            {
                var swap = await satoraService.GetSwapAsync(swapId, storeSettings.Seed);
                model.BackendStatus = swap.Status.GetType().Name;
                model.DepositAddress = (swap.Funding as SwapFunding.Gasless)?.@depositAddress;
                model.DepositAmount = swap.DepositAmount;
                model.DepositToken = swap.DepositToken.GetType().Name;
                model.ReceiveAddress = swap.ReceiveAddress;
                model.ReceiveAmount = swap.ReceiveAmount;
                model.ReceiveToken = swap.ReceiveToken.GetType().Name;
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "SatoraPlugin:GetSwapDetailsAsync({SwapId}): backend lookup failed", swapId);
                model.BackendError = e.Message;
            }

            try
            {
                model.DerivedArkadeAddress = await satoraService.GetArkadeAddressAsync(storeSettings.Seed);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "SatoraPlugin:GetSwapDetailsAsync({SwapId}): derived address lookup failed", swapId);
                // Non-fatal — operator can paste a destination manually.
            }

            return model;
        }

        // Drive a single swap one step forward. Idempotent: safe to call
        // repeatedly. Returns the (action, newStatus) tuple so the caller
        // (manual button today, watcher tomorrow) can surface it.
        //
        // FundSwap + Claim both require the same mnemonic that created the
        // swap. With the current hardcoded seed in Plugin.cs that's fine;
        // once persistence lands we'll re-derive the right Client per swap.
        public async Task<(string action, string status)> ContinueSwapAsync(string swapId, string? destinationOverride)
        {
            try
            {
                using var _context = pluginDbContextFactory.CreateContext();
                var dbSwap = await _context.SatoraTransactions.FirstOrDefaultAsync(s => s.TxID == swapId);
                var storeSettings = await _context.SatoraSettings.FirstOrDefaultAsync(s => s.StoreId == dbSwap.StoreId);

                var swap = await satoraService.GetSwapAsync(swapId, storeSettings.Seed);
                var statusName = swap.Status.GetType().Name;

                string action;
                switch (swap.Status)
                {
                    case SwapStatus.Pending:
                        // Backend hasn't seen our funding userOp yet.
                        // Before submitting it, probe the depositor EOA —
                        // FundSwapAsync requires the source token to
                        // already be there (the customer's ERC-20
                        // transfer to the deposit address). Probing
                        // first means we surface "waiting for the
                        // customer" cleanly instead of letting the SDK
                        // throw a noisy TRANSFER_FROM_FAILED.
                        var deposit = await satoraService.CheckDepositAsync(swapId, storeSettings.Seed);
                        if (!deposit.HasSufficientSourceToken)
                        {
                            logger.LogInformation("SatoraPlugin:ContinueSwap({SwapId}): waiting for customer deposit ({Have}/{Need})",
                                swapId, deposit.SourceTokenBalance, deposit.SourceTokenRequired);
                            action = $"waiting_for_deposit ({deposit.SourceTokenBalance}/{deposit.SourceTokenRequired})";
                            break;
                        }
                        var fundReceipt = await satoraService.FundSwapAsync(swapId, storeSettings.Seed);
                        logger.LogInformation("SatoraPlugin:ContinueSwap({SwapId}): funded, userOpHash={Hash}", swapId, fundReceipt.UserOpHash);
                        action = $"funded:{fundReceipt.UserOpHash}";
                        swap = await satoraService.GetSwapAsync(swapId, storeSettings.Seed);
                        statusName = swap.Status.GetType().Name;
                        break;

                    case SwapStatus.ClientFundingSeen:
                    case SwapStatus.ClientFunded:
                        // Funding userOp seen / confirmed on-chain;
                        // waiting for the server to lock its matching
                        // Arkade VHTLC. No client-side action.
                        action = "waiting_for_server";
                        break;

                    case SwapStatus.ServerFunded:
                        // VHTLC is live — sweep BTC out to destination.
                        var destination = destinationOverride
                            ?? await satoraService.GetArkadeAddressAsync(storeSettings.Seed);
                        var claimReceipt = await satoraService.ClaimAsync(swapId, destination, storeSettings.Seed);
                        logger.LogInformation("SatoraPlugin:ContinueSwap({SwapId}): claimed {Sats} sats to {Dest}, ark_txid={Txid}",
                            swapId, claimReceipt.ClaimAmountSats, destination, claimReceipt.ArkTxid);
                        action = $"claimed:{claimReceipt.ArkTxid}";
                        swap = await satoraService.GetSwapAsync(swapId, storeSettings.Seed);
                        statusName = swap.Status.GetType().Name;
                        break;

                    case SwapStatus.ClientRedeeming:
                        action = "redeeming";
                        break;

                    case SwapStatus.ClientRedeemed:
                    case SwapStatus.ServerRedeemed:
                        action = "complete";
                        break;

                    default:
                        // Terminal refund / expiry / error states.
                        action = "terminal";
                        break;
                }

                if (dbSwap != null && dbSwap.Status != statusName)
                {
                    dbSwap.Status = statusName;
                    _context.SatoraTransactions.Update(dbSwap);
                    await _context.SaveChangesAsync();
                }

                return (action, statusName);
            }
            catch (Exception e)
            {
                logger.LogError(e, "SatoraPlugin:ContinueSwapAsync({SwapId})", swapId);
                throw;
            }
        }

    }
}
