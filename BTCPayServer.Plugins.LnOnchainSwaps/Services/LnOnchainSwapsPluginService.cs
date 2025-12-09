using BTCPayServer.Client.Models;
using BTCPayServer.Configuration;
using BTCPayServer.Controllers;
using BTCPayServer.Data;
using BTCPayServer.HostedServices;
using BTCPayServer.Lightning;
using BTCPayServer.Models.StoreViewModels;
using BTCPayServer.Payments;
using BTCPayServer.Payments.Bitcoin;
using BTCPayServer.Payments.Lightning;
using BTCPayServer.Payouts;
using BTCPayServer.Plugins.LnOnchainSwaps.Data;
using BTCPayServer.Plugins.LnOnchainSwaps.Models;
using BTCPayServer.Services;
using BTCPayServer.Services.Invoices;
using BTCPayServer.Services.Stores;
using BTCPayServer.Services.Wallets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NBitcoin;
using NBitpayClient;
using NBXplorer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using StoreData = BTCPayServer.Data.StoreData;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Services
{
    public class LnOnchainSwapsPluginService(StoreRepository storeRepository,
                                      BTCPayNetworkProvider networkProvider,
                                      BTCPayWalletProvider walletProvider,
                                      WalletHistogramService walletHistogramService,
                                      ILogger<LnOnchainSwapsPluginService> logger,
                                      HttpClient httpClient2,
                                      LightningClientFactoryService lightningClientFactory,
                                      IOptions<LightningNetworkOptions> lightningNetworkOptions,
                                      PaymentMethodHandlerDictionary handlers,
                                      ApplicationDbContextFactory btcPayDbContextFactory,
                                      PayoutMethodHandlerDictionary payoutHandlers,
                                      PullPaymentHostedService pullPaymentHostedService,
                                      ExplorerClientProvider explorerClientProvider,
                                      BoltzHttpService boltzService,
                                      InvoiceRepository invoiceRepository,
                                      UIInvoiceController invoiceController,
                                      LnOnchainSwapsDbContextFactory lnOnchainSwapsDbContextFactory)
    {

        public async Task InitSettings(string storeId)
        {
            try
            {
                using var _context = lnOnchainSwapsDbContextFactory.CreateContext();
                var settings = await _context.Settings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                if (settings == null)
                {
                    settings = new Settings
                    {
                        StoreId = storeId
                    };
                    var mnemonicBoltz = new Mnemonic(Wordlist.English, WordCount.Twelve);
                    settings.RefundMnemonic = mnemonicBoltz.ToString();

                    _context.Settings.Add(settings);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "LnOnchainSwapsPlugin:InitSettings()");
                throw;
            }
        }

        public async Task<Settings> GetStoreSettings(string storeId)
        {
            try
            {
                using var _context = lnOnchainSwapsDbContextFactory.CreateContext();
                var settings = await _context.Settings.FirstAsync(a => a.StoreId == storeId);
                return settings;
            }
            catch (Exception e)
            {
                logger.LogError(e, "LnOnchainSwapsPlugin:GetStoreSettings()");
                throw;
            }
        }

        public async Task<List<BoltzSwap>> GetStoreSwaps(string storeId)
        {
            try
            {
                using var _context = lnOnchainSwapsDbContextFactory.CreateContext();
                var txs = await _context.BoltzSwaps.Where(a => a.StoreId == storeId).ToListAsync();
                return txs.Reverse<BoltzSwap>().ToList();
            }
            catch (Exception e)
            {
                logger.LogError(e, "LnOnchainSwapsPlugin:GetStoreSwaps()");
                throw;
            }
        }

        private async Task<Tuple<string, string>> CreateInvoice(StoreData store, string rootUrl, decimal amount, string network)
        {
            try
            {
                var paymentMethodId = PaymentMethodId.Parse(network);
                var req = new CreateInvoiceRequest
                {
                    Amount = amount,
                    Type = InvoiceType.Standard,
                    Currency = "BTC",
                    Checkout = new InvoiceDataBase.CheckoutOptions
                    {
                        DefaultPaymentMethod = network,
                        PaymentMethods = [network],
                        Expiration = TimeSpan.FromHours(24),
                    },
                    Metadata = new InvoiceMetadata
                    {
                        ItemDesc = "Boltz swap from LnOnchainSwap plugin",
                    }.ToJObject()
                };
                var invoice = await invoiceController.CreateInvoiceCoreRaw(req, store, rootUrl);
                var invDest = invoice.GetPaymentPrompts()
                    .FirstOrDefault(prompt => prompt.PaymentMethodId == paymentMethodId).Destination;
                return Tuple.Create(invoice.Id, invDest);
            }
            catch (Exception e)
            {
                logger.LogError(e, "LnOnchainSwapsPlugin:CreateInvoice()");
                throw;
            }
        }

        private string GetRefundPublicKeyForSwap(string refundMnemonic, int index)
        {
            var mnemonic = new Mnemonic(refundMnemonic, Wordlist.English);
            var masterKey = mnemonic.DeriveExtKey();
            var derivedKey = masterKey.Derive(new KeyPath($"m/44/0/0/0/{index}"));
            return derivedKey.PrivateKey.PubKey.ToHex();
        }

        public async Task<BoltzSwap> DoOnchainToLnSwap (string storeId, string RequestGetAbsoluteRoot, OnChainToLnSwap swap)
        {
            try 
            {
                var store = await storeRepository.FindStore(storeId);

                string invoiceId = string.Empty;
                string lnInvoice = string.Empty;
                if (swap.ToInternalLnWalet)
                {
                    var invoiceTuple = await CreateInvoice(store, RequestGetAbsoluteRoot, swap.BtcAmount, "BTC-LN");
                    invoiceId = invoiceTuple.Item1;
                    lnInvoice = invoiceTuple.Item2;
                }
                else
                {
                    lnInvoice = swap.ExternalLnInvoice;
                }

                using var _context = lnOnchainSwapsDbContextFactory.CreateContext();
                var settings = _context.Settings.First(a => a.StoreId == storeId);
                var nbSwaps = await _context.BoltzSwaps.CountAsync(a => a.StoreId == storeId);
                var refundPubKey = GetRefundPublicKeyForSwap(settings.RefundMnemonic, nbSwaps);

                var bolt11 = BOLT11PaymentRequest.Parse(lnInvoice, Network.Main);
                var paymentHash = bolt11.PaymentHash.ToString();

                var boltz = await boltzService.CreateOnChainToLnSwapAsync(lnInvoice, refundPubKey, paymentHash);
                await CreatePayout(store, boltz);
                boltz.StoreId = store.Id;
                boltz.BTCPayInvoiceId = invoiceId;
                boltz.OriginalAmount = swap.BtcAmount;

                _context.BoltzSwaps.Add(boltz);
                await _context.SaveChangesAsync();

                return boltz;
            }
            catch (Exception e)
            {
                logger.LogError(e, "LnOnchainSwapsPlugin:DoOnchainToLnSwap()");
                throw;
            }

        }

        public async Task<BoltzSwap> DoLnToOnchainSwap(string storeId, string RequestGetAbsoluteRoot, LnToOnChainSwap swap)
        {
            try
            {
                var store = await storeRepository.FindStore(storeId);

                string invoiceId = string.Empty;
                string btcAddress = string.Empty;
                if (swap.ToInternalOnChainWallet)
                {
                    var invoiceTuple = await CreateInvoice(store, RequestGetAbsoluteRoot, swap.BtcAmount, "BTC-CHAIN");
                    invoiceId = invoiceTuple.Item1;
                    btcAddress = invoiceTuple.Item2;
                }
                else
                {
                    btcAddress = swap.ExternalOnChainAddress;
                }

                using var _context = lnOnchainSwapsDbContextFactory.CreateContext();
                var settings = _context.Settings.First(a => a.StoreId == storeId);
                var nbSwaps = await _context.BoltzSwaps.CountAsync(a => a.StoreId == storeId);
                var claimPubKey = GetRefundPublicKeyForSwap(settings.RefundMnemonic, nbSwaps);

                var boltz = await boltzService.CreateLnToOnChainSwapAsync(btcAddress, claimPubKey, swap.BtcAmount);
                boltz.ExpectedAmount = ExtractAmountFromLnInvoice(boltz.Destination);

                await CreatePayout(store, boltz);
                boltz.StoreId = store.Id;
                boltz.BTCPayInvoiceId = invoiceId;

                boltz.OriginalAmount = swap.BtcAmount;

                _context.BoltzSwaps.Add(boltz);
                await _context.SaveChangesAsync();

                return boltz;
            }
            catch (Exception e)
            {
                logger.LogError(e, "LnOnchainSwapsPlugin:DoLnToOnchainSwap()");
                throw;
            }

        }

        public async Task<string> DoGetSwapStatus(string swapId)
        {
            try
            {
                var status = await boltzService.GetSwapStatusAsync(swapId);

                using var _context = lnOnchainSwapsDbContextFactory.CreateContext();
                var dbSwap = await _context.BoltzSwaps.FirstOrDefaultAsync(s => s.SwapId == swapId);
                if (dbSwap.Status != status)
                {
                    dbSwap.Status = status;
                    _context.BoltzSwaps.Update(dbSwap);
                    await _context.SaveChangesAsync();
                }
                return status;
            }
            catch (Exception e)
            {
                logger.LogError(e, "LnOnchainSwapsPlugin:DoGetSwapStatus()");
                throw;
            }
        }

        public async Task CreatePayout(StoreData store, BoltzSwap boltzSwap, CancellationToken cancellationToken = default)
        {
            try
            {
                var payoutMethodId = boltzSwap.Type == BoltzSwap.SwapTypeOnChainToLn ? PayoutMethodId.Parse("BTC-CHAIN") : PayoutMethodId.Parse("BTC-LN");

                var ppRequest = new CreatePullPaymentRequest
                {
                    Name = $"Boltz Swap {boltzSwap.SwapId}",
                    Description = "",
                    Amount = boltzSwap.ExpectedAmount,
                    Currency = "BTC",
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                    PayoutMethods = new[] { payoutMethodId.ToString() }
                };

                var ppId = await pullPaymentHostedService.CreatePullPayment(store, ppRequest);

                await using var btcPayCtx = btcPayDbContextFactory.CreateContext();
                var pp = await btcPayCtx.PullPayments.FindAsync(ppId);
                var blob = pp.GetBlob();

                var payoutHandler = payoutHandlers.TryGet(payoutMethodId);
                if (payoutHandler == null)
                    throw new Exception($"No payout handler found for {payoutMethodId}");

                string error = null;
                IClaimDestination LnOnchainSwapsDestination;

                (LnOnchainSwapsDestination, error) = await payoutHandler.ParseAndValidateClaimDestination(boltzSwap.Destination, blob, cancellationToken);

                if (LnOnchainSwapsDestination == null)
                    throw new Exception($"Destination parsing failed: {error ?? "Unknown error"}");

                var result = await pullPaymentHostedService.Claim(new ClaimRequest
                {
                    Destination = LnOnchainSwapsDestination,
                    PullPaymentId = ppId,
                    ClaimedAmount = boltzSwap.ExpectedAmount,
                    PayoutMethodId = payoutMethodId,
                    StoreId = store.Id,
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
                    case ClaimRequest.ClaimResult.PaymentMethodNotSupported:
                        throw new Exception("Payment Method Not Supported");
                    case ClaimRequest.ClaimResult.Overdraft:
                        throw new Exception("Pull payment: overdraft");
                }

                boltzSwap.BTCPayPullPaymentId = ppId;
                boltzSwap.BTCPayPayoutId = result.PayoutData.Id;
            }
            catch (Exception e)
            {
                logger.LogError(e, "LnOnchainSwapsPlugin:CreatePayout()");
                throw;
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
                        using (var rep = await httpClient2.GetAsync($"rates?storeId={storeId}&currencyPairs=BTC_{cnfg.FiatCurrency}"))
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
                            using (var rep = await _httpClient2.GetAsync($"rates?storeId={storeId}&currencyPairs=BTC_CHF"))
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
                logger.LogError(e, "LnOnchainSwapsPlugin:GetBalances()");
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

        private decimal ExtractAmountFromLnInvoice(string invoice)
        {
            if (string.IsNullOrEmpty(invoice) || !invoice.ToLower().StartsWith("lnbc"))
            {
                return 0;
            }
            int separatorIndex = invoice.LastIndexOf('1');
            if (separatorIndex == -1)
            {
                return 0;
            }
            string hrp = invoice.Substring(0, separatorIndex);
            string dataPart = hrp.Substring(4);
            string amountString = new string(dataPart.TakeWhile(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(amountString))
            {
                return 0;
            }

            decimal amount = decimal.Parse(amountString);
            if (dataPart.Length > amountString.Length)
            {
                char multiplier = dataPart[amountString.Length];
                switch (multiplier)
                {
                    case 'm': // milli-bitcoin (10^-3)
                        amount /= 1_000m;
                        break;
                    case 'u': // micro-bitcoin (10^-6)
                        amount /= 1_000_000m;
                        break;
                    case 'n': // nano-bitcoin (10^-9)
                        amount /= 1_000_000_000m;
                        break;
                    case 'p': // pico-bitcoin (10^-12)
                        amount /= 1_000_000_000_000m;
                        break;
                }
            }
            return amount;
        }
    }
}
