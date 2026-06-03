using BTCPayServer.Plugins.Satora.Data;
using BTCPayServer.Plugins.Satora.Models;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using uniffi.satora_sdk_ffi;

namespace BTCPayServer.Plugins.Satora.Services
{
    public class SatoraPluginService(
        SatoraPluginDbContextFactory pluginDbContextFactory,
        ILogger<SatoraPluginService> logger,
        SatoraService satoraService)
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

                return new SatoraModel { Settings = settings, Transactions = txs };
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
                    settings.Seed = new Mnemonic(Wordlist.English, WordCount.Twelve).ToString();

                await using var _context = pluginDbContextFactory.CreateContext();
                var dbSettings = await _context.SatoraSettings.FirstOrDefaultAsync(a => a.StoreId == settings.StoreId);
                if (dbSettings == null)
                    _context.SatoraSettings.Add(settings);
                else
                {
                    dbSettings.Enabled = settings.Enabled;
                    dbSettings.Seed    = settings.Seed;
                    _context.SatoraSettings.Update(dbSettings);
                }
                await _context.SaveChangesAsync();
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
                var dbSwap    = await _context.SatoraTransactions.FirstOrDefaultAsync(s => s.TxID == swapId);
                var settings  = await _context.SatoraSettings.FirstOrDefaultAsync(s => s.StoreId == dbSwap.StoreId);
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
                await using var _context = pluginDbContextFactory.CreateContext();
                var dbSwap      = await _context.SatoraTransactions.FirstOrDefaultAsync(s => s.TxID == swapId);
                var storeSettings = await _context.SatoraSettings.FirstOrDefaultAsync(s => s.StoreId == dbSwap.StoreId);
                var status      = await satoraService.GetSwapInfoAsync(swapId, storeSettings.Seed);
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
                model.BackendStatus  = swap.Status.GetType().Name;
                model.DepositAddress = (swap.Funding as SwapFunding.Gasless)?.@depositAddress;
                model.DepositAmount  = swap.DepositAmount;
                model.DepositToken   = swap.DepositToken.GetType().Name;
                model.ReceiveAddress = swap.ReceiveAddress;
                model.ReceiveAmount  = swap.ReceiveAmount;
                model.ReceiveToken   = swap.ReceiveToken.GetType().Name;
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
            }

            return model;
        }


        public async Task<(string action, string status)> ContinueSwapAsync(string swapId, string? destinationOverride = null)
        {
            try
            {
                await using var _context = pluginDbContextFactory.CreateContext();
                var dbSwap        = await _context.SatoraTransactions.FirstOrDefaultAsync(s => s.TxID == swapId);
                var storeSettings = await _context.SatoraSettings.FirstOrDefaultAsync(s => s.StoreId == dbSwap.StoreId);

                var swap            = await satoraService.GetSwapAsync(swapId, storeSettings.Seed);
                var statusName      = swap.Status.GetType().Name;
                var originalClaimTxId = dbSwap?.ClaimTxId;
                var originalSweepTxId = dbSwap?.SweepTxId;

                string action;
                switch (swap.Status)
                {
                    case SwapStatus.Pending:
                        var deposit = await satoraService.CheckDepositAsync(swapId, storeSettings.Seed);
                        if (!deposit.HasSufficientSourceToken)
                        {
                            action = $"waiting_for_deposit ({deposit.SourceTokenBalance}/{deposit.SourceTokenRequired})";
                            break;
                        }
                        var fundReceipt = await satoraService.FundSwapAsync(swapId, storeSettings.Seed);
                        logger.LogInformation("SatoraPlugin:ContinueSwap({SwapId}): funded, userOpHash={Hash}", swapId, fundReceipt.UserOpHash);
                        action     = $"funded:{fundReceipt.UserOpHash}";
                        swap       = await satoraService.GetSwapAsync(swapId, storeSettings.Seed);
                        statusName = swap.Status.GetType().Name;
                        break;

                    case SwapStatus.ClientFundingSeen:
                    case SwapStatus.ClientFunded:
                        action = "waiting_for_server";
                        break;

                    case SwapStatus.ServerFunded:
                        var destination = destinationOverride
                            ?? await satoraService.GetArkadeAddressAsync(storeSettings.Seed);
                        var claimReceipt = await satoraService.ClaimAsync(swapId, destination, storeSettings.Seed);
                        logger.LogInformation("SatoraPlugin:ContinueSwap({SwapId}): claimed {Sats} sats → wallet Satora, ark_txid={Txid}",
                            swapId, claimReceipt.ClaimAmountSats, claimReceipt.ArkTxid);
                        if (dbSwap != null)
                            dbSwap.ClaimTxId = claimReceipt.ArkTxid;

                        var sweepTxId = await SweepToMerchantAsync(
                            dbSwap?.BTCPayInvoiceId,
                            claimReceipt.ClaimAmountSats,
                            storeSettings.Seed,
                            swapId,
                            dbSwap?.InvoiceArkadeAddress);

                        if (sweepTxId != null && dbSwap != null)
                            dbSwap.SweepTxId = sweepTxId;

                        action     = sweepTxId != null ? $"swept:{sweepTxId}" : $"claimed:{claimReceipt.ArkTxid}";
                        swap       = await satoraService.GetSwapAsync(swapId, storeSettings.Seed);
                        statusName = swap.Status.GetType().Name;
                        break;

                    case SwapStatus.ClientRedeeming:
                        action = "redeeming";
                        break;

                    case SwapStatus.ClientRedeemed:
                    case SwapStatus.ServerRedeemed:
                        if (dbSwap != null && !string.IsNullOrEmpty(dbSwap.ClaimTxId) && string.IsNullOrEmpty(dbSwap.SweepTxId))
                        {
                            ulong.TryParse(swap.ReceiveAmount, out var redeemedSats);
                            var retrySweepTxId = await SweepToMerchantAsync(
                                dbSwap.BTCPayInvoiceId,
                                redeemedSats,
                                storeSettings.Seed,
                                swapId,
                                dbSwap.InvoiceArkadeAddress);

                            if (retrySweepTxId != null)
                                dbSwap.SweepTxId = retrySweepTxId;
                        }
                        action = "complete";
                        break;

                    default:
                        action = "terminal";
                        break;
                }

                if (dbSwap != null && (
                    dbSwap.Status     != statusName      ||
                    dbSwap.ClaimTxId  != originalClaimTxId ||
                    dbSwap.SweepTxId  != originalSweepTxId))
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

        private async Task<string?> SweepToMerchantAsync(
            string? invoiceId,
            ulong amountSats,
            string satoraSeed,
            string swapId,
            string? invoiceAddress)
        {
            try
            {

                var sweepTxId = await satoraService.SweepToAddressAsync(invoiceAddress ?? "", amountSats, satoraSeed);
                logger.LogInformation(
                    "SatoraPlugin:SweepToMerchant({SwapId}): {Sats} sats swept to {Dest}, sweep_txid={Txid}",
                    swapId, amountSats, invoiceAddress, sweepTxId);
                return sweepTxId;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "SatoraPlugin:SweepToMerchant({SwapId}): sweep failed — " +
                    "{Sats} sats remain in the Satora wallet. " +
                    "The claim is preserved, the sweep will be retried on the next tick.",
                    swapId, amountSats);
                return null;
            }
        }
    }
}
