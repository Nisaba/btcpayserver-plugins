using AngleSharp.Dom;
using BTCPayServer.Client.Models;
using BTCPayServer.Configuration;
using BTCPayServer.Controllers;
using BTCPayServer.Data;
using BTCPayServer.HostedServices;
using BTCPayServer.Lightning;
using BTCPayServer.Logging;
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
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NBitcoin;
using NBitpayClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
                                      PullPaymentHostedService pullPaymentService,
                                      PaymentMethodHandlerDictionary handlers,
                                      ApplicationDbContextFactory btcPayDbContextFactory,
                                      PayoutMethodHandlerDictionary payoutHandlers,
                                      PullPaymentHostedService pullPaymentHostedService,
                                      ExplorerClientProvider explorerClientProvider,
                                      BoltzService boltzService,
                                      InvoiceRepository invoiceRepository,
                                      UIInvoiceController invoiceController,
                                      LnOnchainSwapsDbContext context)
    {
        private readonly ILogger<LnOnchainSwapsPluginService> _logger = logger;
        private readonly StoreRepository _storeRepository = storeRepository;
        private readonly BTCPayNetworkProvider _networkProvider = networkProvider;
        private readonly WalletHistogramService _walletHistogramService = walletHistogramService;
        private readonly BTCPayWalletProvider _walletProvider = walletProvider;
        private readonly PaymentMethodHandlerDictionary _handlers = handlers;
        private readonly HttpClient _httpClient2 = httpClient2;
        private readonly LightningClientFactoryService _lightningClientFactory = lightningClientFactory;
        private readonly IOptions<LightningNetworkOptions> _lightningNetworkOptions = lightningNetworkOptions;
        private readonly PullPaymentHostedService _pullPaymentService = pullPaymentService;
        private readonly ApplicationDbContextFactory _btcPayDbContextFactory = btcPayDbContextFactory;
        private readonly PayoutMethodHandlerDictionary _payoutHandlers= payoutHandlers;
        private readonly PullPaymentHostedService _pullPaymentHostedService = pullPaymentHostedService;
        private readonly ExplorerClientProvider _explorerClientProvider = explorerClientProvider;
        private readonly BoltzService _boltzService = boltzService;
        private readonly InvoiceRepository _invoiceRepository = invoiceRepository;
        private readonly UIInvoiceController _invoiceController = invoiceController;
        private readonly LnOnchainSwapsDbContext _context = context;


        public async Task<List<BoltzSwap>> GetStoreSwaps(string storeId)
        {
            try
            {
                var txs = await _context.BoltzSwaps.Where(a => a.StoreId == storeId).ToListAsync();
                return txs.Reverse<BoltzSwap>().ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "LnOnchainSwapsPlugin:GetStoreSwaps()");
                throw;
            }
        }

        private async Task<string> CreateInvoice(StoreData store, string rootUrl, decimal amount, string network)
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
                        PaymentMethods = [network]
                    }
                };
                var invoice = await _invoiceController.CreateInvoiceCoreRaw(req, store, rootUrl);
                var invDest = invoice.GetPaymentPrompts()
                    .FirstOrDefault(prompt => prompt.PaymentMethodId == paymentMethodId).Destination;
                return invDest;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "LnOnchainSwapsPlugin:CreateInvoice()");
                throw;
            }
        }

        public async Task<BoltzSwap> DoOnchainToLnSwap (string storeId, string RequestGetAbsoluteRoot, OnChainToLnSwap swap)
        {
            try 
            {
                var store = await _storeRepository.FindStore(storeId);

                var lnInvoice = swap.ToInternalLnWalet ? 
                                await CreateInvoice(store, RequestGetAbsoluteRoot, swap.BtcAmount, "BTC-LN")
                                : swap.ExternalLnInvoice;

                var boltz = await _boltzService.CreateOnChainToLnSwapAsync(lnInvoice);
                await CreatePayout(store, boltz);

                _context.BoltzSwaps.Add(boltz);
                await _context.SaveChangesAsync();

                return boltz;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "LnOnchainSwapsPlugin:DoOnchainToLnSwap()");
                throw;
            }

        }

        public async Task<BoltzSwap> DoLnToOnchainSwap(string storeId, string RequestGetAbsoluteRoot, LnToOnChainSwap swap)
        {
            try
            {
                var store = await _storeRepository.FindStore(storeId);

                var btcAddress = swap.ToInternalOnChainWallet?
                                await CreateInvoice(store, RequestGetAbsoluteRoot, swap.BtcAmount, "BTC-CHAIN")
                                : swap.ExternalOnChainAddress;

                var boltz = await _boltzService.CreateLnToOnChainSwapAsync(btcAddress, swap.BtcAmount);
                await CreatePayout(store, boltz);

                _context.BoltzSwaps.Add(boltz);
                await _context.SaveChangesAsync();

                return boltz;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "LnOnchainSwapsPlugin:DoLnToOnchainSwap()");
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

                var ppId = await _pullPaymentService.CreatePullPayment(store, ppRequest);

                await using var btcPayCtx = _btcPayDbContextFactory.CreateContext();
                var pp = await btcPayCtx.PullPayments.FindAsync(ppId);
                var blob = pp.GetBlob();

                var payoutHandler = _payoutHandlers.TryGet(payoutMethodId);
                if (payoutHandler == null)
                    throw new Exception($"No payout handler found for {payoutMethodId}");

                string error = null;
                IClaimDestination LnOnchainSwapsDestination;

                (LnOnchainSwapsDestination, error) = await payoutHandler.ParseAndValidateClaimDestination(boltzSwap.Destination, blob, cancellationToken);

                if (LnOnchainSwapsDestination == null)
                    throw new Exception($"Destination parsing failed: {error ?? "Unknown error"}");

                var result = await _pullPaymentHostedService.Claim(new ClaimRequest
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
                }

                boltzSwap.BTCPayPayoutId = result.PayoutData.Id;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "LnOnchainSwapsPlugin:CreatePayout()");
                throw;
            }
        }

        public async Task<StoreWalletConfig> GetBalances(string storeId, string BaseUrl)
        {
            StoreWalletConfig cnfg = new StoreWalletConfig();
            try
            {
                var store = await _storeRepository.FindStore(storeId);
                var blob = store.GetStoreBlob();

                cnfg.FiatCurrency = blob.DefaultCurrency;
                if (_networkProvider.DefaultNetwork.IsBTC)
                {
                    getPaymentMethods(store, blob,
                        out var derivationSchemes, out var lightningNodes);

                    cnfg.OffChainEnabled = lightningNodes.Any(ln => !string.IsNullOrEmpty(ln.Address) && ln.Enabled);
                    cnfg.OnChainEnabled = derivationSchemes.Any(scheme => !string.IsNullOrEmpty(scheme.Value) && scheme.Enabled);

                    if (cnfg.OnChainEnabled)
                    {
                        var walletId = new WalletId(store.Id, "BTC");
                        var data = await _walletHistogramService.GetHistogram(store, walletId, HistogramType.Week);
                        if (data != null)
                        {
                            cnfg.OnChainBalance = data.Balance;
                        }
                        else
                        {
                            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(3));
                            var wallet = _walletProvider.GetWallet(_networkProvider.DefaultNetwork);
                            var derivation = store.GetDerivationSchemeSettings(_handlers, walletId.CryptoCode);
                            if (derivation is not null)
                            {
                                var network = _handlers.GetBitcoinHandler(walletId.CryptoCode).Network;
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
                        if (_httpClient2.BaseAddress == null)
                        {
                            _httpClient2.BaseAddress = new Uri($"{BaseUrl}/api/");
                        }
                        string sRep;
                        using (var rep = await _httpClient2.GetAsync($"rates?storeId={storeId}&currencyPairs=BTC_{cnfg.FiatCurrency}"))
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
                _logger.LogError(e, "LnOnchainSwapsPlugin:GetBalances()");
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
                    .GetPaymentMethodConfigs<DerivationSchemeSettings>(_handlers)
                    .ToDictionary(c => ((IHasNetwork)_handlers[c.Key]).Network.CryptoCode, c => c.Value);

            var lightningByCryptoCode = store
                .GetPaymentMethodConfigs(_handlers)
                .Where(c => c.Value is LightningPaymentMethodConfig)
                .ToDictionary(c => ((IHasNetwork)_handlers[c.Key]).Network.CryptoCode, c => (LightningPaymentMethodConfig)c.Value);

            derivationSchemes = new List<StoreDerivationScheme>();
            lightningNodes = new List<StoreLightningNode>();

            foreach (var handler in _handlers)
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
            var network = _networkProvider.GetNetwork<BTCPayNetwork>("BTC");
            var id = PaymentTypes.LN.GetPaymentMethodId("BTC");
            var existing = store.GetPaymentMethodConfig<LightningPaymentMethodConfig>(id, _handlers);
            if (existing == null)
                return null;

            if (existing.GetExternalLightningUrl() is { } connectionString)
            {
                return _lightningClientFactory.Create(connectionString, network);
            }
            if (existing.IsInternalNode && _lightningNetworkOptions.Value.InternalLightningByCryptoCode.TryGetValue("BTC", out var internalLightningNode))
            {
                return internalLightningNode;
            }

            return null;
        }
    }
}
