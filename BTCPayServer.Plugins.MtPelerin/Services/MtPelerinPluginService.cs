using BTCPayServer.Client.Models;
using BTCPayServer.Configuration;
using BTCPayServer.Data;
using BTCPayServer.HostedServices;
using BTCPayServer.Lightning;
using BTCPayServer.Models.StoreViewModels;
using BTCPayServer.Payments;
using BTCPayServer.Payments.Bitcoin;
using BTCPayServer.Payments.Lightning;
using BTCPayServer.Payouts;
using BTCPayServer.Plugins.MtPelerin.Data;
using BTCPayServer.Plugins.MtPelerin.Model;
using BTCPayServer.Services;
using BTCPayServer.Services.Invoices;
using BTCPayServer.Services.Stores;
using BTCPayServer.Services.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NBitcoin;
using NBXplorer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.MtPelerin.Services
{
    public class MtPelerinPluginService(MtPelerinPluginDbContextFactory pluginDbContextFactory,
                                      StoreRepository storeRepository,
                                      BTCPayNetworkProvider networkProvider,
                                      BTCPayWalletProvider walletProvider,
                                      WalletHistogramService walletHistogramService,
                                      ILogger<MtPelerinPluginService> logger,
                                      HttpClient httpClient2,
                                      LightningClientFactoryService lightningClientFactory,
                                      IOptions<LightningNetworkOptions> lightningNetworkOptions,
                                      PaymentMethodHandlerDictionary handlers,
                                      ApplicationDbContextFactory btcPayDbContextFactory,
                                      PayoutMethodHandlerDictionary payoutHandlers,
                                      PullPaymentHostedService pullPaymentHostedService,
                                      ExplorerClientProvider explorerClientProvider)
    {

        public async Task<MtPelerinSettings> GetStoreSettings(string storeId)
        {
            try
            {
                using var context = pluginDbContextFactory.CreateContext();
                var settings = await context.MtPelerinSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                if (settings == null)
                {
                    settings = new MtPelerinSettings { StoreId = storeId, Lang = "en", Phone = string.Empty, UseBridgeApp = false };
                }
                return settings;

            }
            catch (Exception e)
            {
                logger.LogError(e, "MtPelerinPlugin:GetStoreSettings()");
                throw;
            }
        }

        public async Task UpdateSettings(MtPelerinSettings settings)
        {
            try
            {
                using var context = pluginDbContextFactory.CreateContext();
                var dbSettings = await context.MtPelerinSettings.FirstOrDefaultAsync(a => a.StoreId == settings.StoreId);
                if (dbSettings == null)
                {
                    context.MtPelerinSettings.Add(settings);
                }
                else
                {
                    dbSettings.Lang = settings.Lang;
                    dbSettings.UseBridgeApp = settings.UseBridgeApp;
                    dbSettings.Phone = settings.Phone;
                    context.MtPelerinSettings.Update(dbSettings);
                }

                await context.SaveChangesAsync();
                return;

            }
            catch (Exception e)
            {
                logger.LogError(e, "MtPelerinPlugin:UpdateSettings()");
                throw;
            }
        }
        public async Task CreatePayout(string storeId, MtPelerinOperation operation, CancellationToken cancellationToken = default)
        {
            try
            {
                var payoutMethodId = operation.IsOnChain ?
                                        PayoutMethodId.Parse("BTC-CHAIN") :
                                        PayoutMethodId.Parse("BTC-LN");

                var ppRequest = new CreatePullPaymentRequest
                {
                    Name = $"Mt Pelerin {operation.Type} {operation.MtPelerinId}",
                    Description = "",
                    Amount = operation.Amount,
                    Currency = "BTC",
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                    PayoutMethods = new[] { payoutMethodId.ToString() }
                };

                var store = await storeRepository.FindStore(storeId);
                var ppId = await pullPaymentHostedService.CreatePullPayment(store, ppRequest);

                await using var btcPayCtx = btcPayDbContextFactory.CreateContext();
                var pp = await btcPayCtx.PullPayments.FindAsync(ppId);
                var blob = pp.GetBlob();

                var payoutHandler = payoutHandlers.TryGet(payoutMethodId);
                if (payoutHandler == null)
                    throw new Exception($"No payout handler found for {payoutMethodId}");

                string error = null;
                var sDest = operation.IsOnChain
                    ? MtPelerinSettings.BtcDestAdress
                    : operation.LnInvoice;
                IClaimDestination mtPelerinDestination;

                (mtPelerinDestination, error) = await payoutHandler.ParseAndValidateClaimDestination(sDest, blob, cancellationToken);

                if (mtPelerinDestination == null)
                    throw new Exception($"Destination parsing failed: {error ?? "Unknown error"}");

               // (mtPelerinDestination, error) = await payoutHandler.ParseAndValidateClaimDestination(sDest, blob, cancellationToken);

                var result = await pullPaymentHostedService.Claim(new ClaimRequest
                {
                    Destination = mtPelerinDestination,
                    PullPaymentId = ppId,
                    ClaimedAmount = operation.Amount,
                    PayoutMethodId = payoutMethodId,
                    StoreId = storeId,
                    PreApprove = true,
                });

                switch (result.Result)
                {
                    case ClaimRequest.ClaimResult.Duplicate:
                        throw new Exception("Duplicate claim for pull payment");
                    case ClaimRequest.ClaimResult.Expired:
                        throw new Exception("Pull payment expired");
                    case ClaimRequest.ClaimResult.Archived:
                        throw new Exception("Pull payment archived");
                    case ClaimRequest.ClaimResult.AmountTooLow:
                        throw new Exception("Claim amount is too low");
                    case ClaimRequest.ClaimResult.NotStarted:
                        throw new Exception("Pull payment has not started yet");
                }
                ;
            }
            catch (Exception e)
            {
                logger.LogError(e, "MtPelerinPlugin:CreatePayout()");
                throw;
            }
        }

        public async Task<MtPelerinSigningInfo> GetSigningAdressInfo(string storeId)
        {
            var signInfo = new MtPelerinSigningInfo
            {
                Code = 0,
                Signature = string.Empty,
                SenderBtcAddress = string.Empty
            };

            try
            {
                var store = await storeRepository.FindStore(storeId);

                var walletId = new WalletId(store.Id, "BTC");
                var derivationScheme = store.GetDerivationSchemeSettings(handlers, walletId.CryptoCode);
                if (derivationScheme == null)
                    return signInfo;

                var btcNetwork = networkProvider.DefaultNetwork as BTCPayNetwork;
                if (btcNetwork == null)
                    return signInfo;

                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(3));
                var wallet = walletProvider.GetWallet(btcNetwork);
                var utxos = await wallet.GetUnspentCoins(derivationScheme.AccountDerivation);
                if (utxos.Length == 0)
                    return signInfo;

                var utxo = utxos.OrderByDescending(u => u.Value).FirstOrDefault();
                signInfo.SenderBtcAddress = utxo.Address.ToString();

                var explorer = explorerClientProvider.GetExplorerClient(btcNetwork);
                var masterKeyString = await explorer.GetMetadataAsync<string>(
                    derivationScheme.AccountDerivation,
                    WellknownMetadataKeys.MasterHDKey);

                if (!string.IsNullOrEmpty(masterKeyString) && utxo.KeyPath != null)
                {
                    var extKey = ExtKey.Parse(masterKeyString, btcNetwork.NBitcoinNetwork);

                    var accountKeyPath = new KeyPath("m/84'/0'/0'");
                    var fullKeyPath = accountKeyPath.Derive(utxo.KeyPath);
                    var derivedKey = extKey.Derive(fullKeyPath);
                    var derivedAddress = derivedKey.PrivateKey.PubKey.GetAddress(ScriptPubKeyType.Segwit, btcNetwork.NBitcoinNetwork);
                    // logger.LogInformation($"Derived Address: {derivedAddress}");
                    // logger.LogInformation($"Expected Address: {signInfo.SenderBtcAddress}");

                    signInfo.Code = new Random().Next(1000, 9999);
                    var messageToSign = "MtPelerin-" + signInfo.Code;
                    signInfo.Signature = SignBitcoinMessage(derivedKey.PrivateKey, messageToSign);
                    //  logger.LogInformation($"Signing message: {messageToSign} with address {signInfo.SenderBtcAddress}");
                    // logger.LogInformation($"Signature: {signInfo.Signature} (derivedKey)");
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "MtPelerinPlugin:GetSigningAdressInfo()");
            }
            return signInfo;
        }



        private string SignBitcoinMessage(Key key, string message)
        {
            var magic = "Bitcoin Signed Message:\n";
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            WriteVarString(writer, magic);
            WriteVarString(writer, message);
            var messageBytes = ms.ToArray();
            var hash = NBitcoin.Crypto.Hashes.DoubleSHA256(messageBytes);

            var compactSig = key.SignCompact(hash, key.PubKey.IsCompressed);
            byte[] signatureBytes = new byte[65];
            signatureBytes[0] = (byte)(27 + compactSig.RecoveryId + (key.PubKey.IsCompressed ? 4 : 0));
            Array.Copy(compactSig.Signature, 0, signatureBytes, 1, 64);

            return Convert.ToBase64String(signatureBytes);
        }

        private void WriteVarString(BinaryWriter writer, string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            WriteVarInt(writer, bytes.Length);
            writer.Write(bytes);
        }

        private void WriteVarInt(BinaryWriter writer, int value)
        {
            if (value < 0xfd)
                writer.Write((byte)value);
            else if (value <= 0xffff)
            {
                writer.Write((byte)0xfd);
                writer.Write((ushort)value);
            }
            else
            {
                writer.Write((byte)0xfe);
                writer.Write(value);
            }
        }


        public async Task<StoreWalletConfig> GetBalances(string storeId, string BaseUrl)
        {
            StoreWalletConfig cnfg = new StoreWalletConfig();
            try
            {
                var store = await storeRepository.FindStore(storeId);
                var blob = store.GetStoreBlob();

                cnfg.FiatCurrency = blob.DefaultCurrency;
                if (networkProvider.DefaultNetwork.IsBTC)
                {
                    getPaymentMethods(store, blob,
                        out var derivationSchemes, out var lightningNodes);

                    cnfg.OffChainEnabled = lightningNodes.Any(ln => !string.IsNullOrEmpty(ln.Address) && ln.Enabled);
                    cnfg.OnChainEnabled = derivationSchemes.Any(scheme => !string.IsNullOrEmpty(scheme.Value) && scheme.Enabled);

                    if (cnfg.OnChainEnabled)
                    {
                        var walletId = new WalletId(store.Id, "BTC");
                        var data = await walletHistogramService.GetHistogram(store, walletId, HistogramType.Week);
                        if (data != null)
                        {
                            cnfg.OnChainBalance = data.Balance;
                        }
                        else
                        {
                            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(3));
                            var wallet = walletProvider.GetWallet(networkProvider.DefaultNetwork);
                            var derivation = store.GetDerivationSchemeSettings(handlers, walletId.CryptoCode);
                            if (derivation is not null)
                            {
                                var network = handlers.GetBitcoinHandler(walletId.CryptoCode).Network;
                                var balance = await wallet.GetBalance(derivation.AccountDerivation, cts.Token);
                                cnfg.OnChainBalance = balance.Available.GetValue(network);
                            }
                        }
                    }

                    if (cnfg.OffChainEnabled)
                    {
                        try
                        {
                            var lightningClient = GetLightningClient(store);
                            var balance = await lightningClient.GetBalance();
                            cnfg.OffChainBalance = (balance.OffchainBalance != null
                                               ? (balance.OffchainBalance.Local ?? 0) : 0).ToDecimal(LightMoneyUnit.BTC);
                            var info = await lightningClient.GetInfo();
                            if (info.Alias == "boltz-client" && balance.OnchainBalance != null)
                            {
                                var totalOnchain = (balance.OnchainBalance.Confirmed ?? 0L) + (balance.OnchainBalance.Reserved ?? 0L) +
                                                      (balance.OnchainBalance.Unconfirmed ?? 0L);
                                cnfg.OffChainBalance += Convert.ToDecimal(totalOnchain) / 100000000;
                            }
                        }
                        catch { }
                    }

                    if (cnfg.OnChainBalance > 0 || cnfg.OffChainBalance > 0)
                    {
                        if (httpClient2.BaseAddress == null)
                        {
                            httpClient2.BaseAddress = new Uri($"{BaseUrl}/api/");
                        }
                        string sRep;
                        using (var rep = await httpClient2.GetAsync($"rates?storeId={storeId}&currencyPairs=BTC{cnfg.FiatCurrency}"))
                        {
                            rep.EnsureSuccessStatusCode();
                            sRep = await rep.Content.ReadAsStringAsync();
                        }
                        dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);
                        string rate = JsonRep[0].rate;
                        cnfg.Rate = decimal.Parse(rate);

                        cnfg.OffChainFiatBalance = cnfg.Rate * cnfg.OffChainBalance;
                        cnfg.OnChainFiatBalance = cnfg.Rate * cnfg.OnChainBalance;

                        /*if (cnfg.FiatCurrency == "CHF")
                        {
                            cnfg.ChfRate = cnfg.Rate;
                        } else
                        {
                            using (var rep = await httpClient2.GetAsync($"rates?storeId={storeId}&currencyPairs=BTCCHF"))
                            {
                                rep.EnsureSuccessStatusCode();
                                sRep = await rep.Content.ReadAsStringAsync();
                            }
                            dynamic JsonRep2 = JsonConvert.DeserializeObject<dynamic>(sRep);
                            string rate2 = JsonRep[0].rate;
                            cnfg.ChfRate = decimal.Parse(rate);
                        }*/
                    }

                }
                else
                {
                    cnfg.OffChainEnabled = false;
                    cnfg.OnChainEnabled = false;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "MtPelerinPlugin:GetBalances()");
                //            throw;
            }
            return cnfg;

        }

        private void getPaymentMethods(BTCPayServer.Data.StoreData store, StoreBlob storeBlob,
            out List<StoreDerivationScheme> derivationSchemes, out List<StoreLightningNode> lightningNodes)
        {
            var excludeFilters = storeBlob.GetExcludedPaymentMethods();
            var derivationByCryptoCode =
                store
                    .GetPaymentMethodConfigs<DerivationSchemeSettings>(handlers)
                    .ToDictionary(c => ((IHasNetwork)handlers[c.Key]).Network.CryptoCode, c => c.Value);

            var lightningByCryptoCode = store
                .GetPaymentMethodConfigs(handlers)
                .Where(c => c.Value is LightningPaymentMethodConfig)
                .ToDictionary(c => ((IHasNetwork)handlers[c.Key]).Network.CryptoCode, c => (LightningPaymentMethodConfig)c.Value);

            derivationSchemes = new List<StoreDerivationScheme>();
            lightningNodes = new List<StoreLightningNode>();

            foreach (var handler in handlers)
            {
                if (handler is BitcoinLikePaymentHandler { Network: var network })
                {
                    var strategy = derivationByCryptoCode.TryGet(network.CryptoCode);
                    var value = strategy?.ToPrettyString() ?? string.Empty;
                    derivationSchemes.Add(new StoreDerivationScheme
                    {
                        Crypto = network.CryptoCode,
                        PaymentMethodId = handler.PaymentMethodId,
                        WalletSupported = network.WalletSupported,
                        Value = value,
                        WalletId = new WalletId(store.Id, network.CryptoCode),
                        Enabled = !excludeFilters.Match(handler.PaymentMethodId) && strategy != null,
                        Collapsed = network is Plugins.Altcoins.ElementsBTCPayNetwork { IsNativeAsset: false } && string.IsNullOrEmpty(value)

                    });
                }
                else if (handler is LightningLikePaymentHandler)
                {
                    var lnNetwork = ((IHasNetwork)handler).Network;
                    var lightning = lightningByCryptoCode.TryGet(lnNetwork.CryptoCode);
                    var isEnabled = !excludeFilters.Match(handler.PaymentMethodId) && lightning != null;
                    lightningNodes.Add(new StoreLightningNode
                    {
                        CryptoCode = lnNetwork.CryptoCode,
                        PaymentMethodId = handler.PaymentMethodId,
                        Address = lightning?.GetDisplayableConnectionString(),
                        Enabled = isEnabled
                    });
                }
            }
        }
        private ILightningClient GetLightningClient(BTCPayServer.Data.StoreData store)
        {
            var network = networkProvider.GetNetwork<BTCPayNetwork>("BTC");
            var id = PaymentTypes.LN.GetPaymentMethodId("BTC");
            var existing = store.GetPaymentMethodConfig<LightningPaymentMethodConfig>(id, handlers);
            if (existing == null)
                return null;

            if (existing.GetExternalLightningUrl() is { } connectionString)
            {
                return lightningClientFactory.Create(connectionString, network);
            }
            if (existing.IsInternalNode && lightningNetworkOptions.Value.InternalLightningByCryptoCode.TryGetValue("BTC", out var internalLightningNode))
            {
                return internalLightningNode;
            }

            return null;
        }
    }
}
