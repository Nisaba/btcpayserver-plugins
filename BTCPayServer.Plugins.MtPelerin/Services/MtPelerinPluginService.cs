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
using BTCPayServer.Plugins.MtPelerin.Model;
using BTCPayServer.Services;
using BTCPayServer.Services.Invoices;
using BTCPayServer.Services.Stores;
using BTCPayServer.Services.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NBitcoin;
using NBitcoin.Protocol;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PayoutData = BTCPayServer.Data.PayoutData;
using PullPaymentData = BTCPayServer.Data.PullPaymentData;

namespace BTCPayServer.Plugins.MtPelerin.Services
{
    public class MtPelerinPluginService
    {
        private readonly ILogger<MtPelerinPluginService> _logger;
        private readonly StoreRepository _storeRepository;
        private readonly MtPelerinPluginDbContext _context;
        private readonly BTCPayNetworkProvider _networkProvider;
        private readonly WalletHistogramService _walletHistogramService;
        private readonly BTCPayWalletProvider _walletProvider;
        private readonly PaymentMethodHandlerDictionary _handlers;
        private readonly HttpClient _httpClient2;
        private readonly LightningClientFactoryService _lightningClientFactory;
        private readonly IOptions<LightningNetworkOptions> _lightningNetworkOptions;
        private readonly PullPaymentHostedService _pullPaymentService;
        private readonly ApplicationDbContextFactory _btcPayDbContextFactory;
        private readonly PayoutMethodHandlerDictionary _payoutHandlers;
        private readonly PullPaymentHostedService _pullPaymentHostedService;

        public MtPelerinPluginService(MtPelerinPluginDbContextFactory pluginDbContextFactory,
                                      StoreRepository storeRepository,
                                      BTCPayNetworkProvider networkProvider,
                                      BTCPayWalletProvider walletProvider,
                                      WalletHistogramService walletHistogramService,
                                      ILogger<MtPelerinPluginService> logger,
                                      HttpClient httpClient2,
                                      LightningClientFactoryService lightningClientFactory,
                                      IOptions<LightningNetworkOptions> lightningNetworkOptions,
                                      PullPaymentHostedService pullPaymentService,
                                      PaymentMethodHandlerDictionary handlers,
                                      ApplicationDbContextFactory btcPayDbContextFactory,
                                      PayoutMethodHandlerDictionary payoutHandlers,
                                      PullPaymentHostedService pullPaymentHostedService)
        {
            _logger = logger;
            _context = pluginDbContextFactory.CreateContext();
            _networkProvider = networkProvider;
            _storeRepository = storeRepository;
            _walletHistogramService = walletHistogramService;
            _walletProvider = walletProvider;
            _handlers = handlers;
            _httpClient2 = httpClient2;
            _lightningClientFactory = lightningClientFactory;
            _lightningNetworkOptions = lightningNetworkOptions;
            _pullPaymentService = pullPaymentService;
            _btcPayDbContextFactory = btcPayDbContextFactory;
            _payoutHandlers = payoutHandlers;
            _pullPaymentHostedService = pullPaymentHostedService;
        }

        public async Task<MtPelerinSettings> GetStoreSettings(string storeId)
        {
            try
            {
                var settings = await _context.MtPelerinSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                if (settings == null)
                {
                    settings = new MtPelerinSettings { StoreId = storeId, Lang = "en", Phone = string.Empty };
                }
                return settings;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "MtPelerinPlugin:GetStoreSettings()");
                throw;
            }
        }

        public async Task UpdateSettings(MtPelerinSettings settings)
        {
            try
            {
                var dbSettings = await _context.MtPelerinSettings.FirstOrDefaultAsync(a => a.StoreId == settings.StoreId);
                if (dbSettings == null)
                {
                    _context.MtPelerinSettings.Add(settings);
                }
                else
                {
                    dbSettings.Lang = settings.Lang;
                    dbSettings.Phone = settings.Phone;
                    _context.MtPelerinSettings.Update(dbSettings);
                }

                await _context.SaveChangesAsync();
                return;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "MtPelerinPlugin:UpdateSettings()");
                throw;
            }
        }

        public async Task CreatePayout(string storeId, decimal amount, bool isOnChain, CancellationToken cancellationToken = default)
        {
            try { 
                var payoutMethodId = isOnChain ?
                                        PayoutMethodId.TryParse("BTC-CHAIN") :
                                        PayoutMethodId.TryParse("BTC-LN");

                var ppRequest = new CreatePullPayment
                {
                    Name = "Mt Pelerin 2",
                    Description = "",
                    Amount = amount,
                    Currency = "BTC",
                    StoreId = storeId,
                    PayoutMethods = new[] { payoutMethodId },
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                };
            
                var ppId = await _pullPaymentService.CreatePullPayment(ppRequest);
            
                var btcPayCtx = _btcPayDbContextFactory.CreateContext();
                var pp = await btcPayCtx.PullPayments.FindAsync(ppId);

                var ppBlob = pp.GetBlob();
                var payoutHandler = _payoutHandlers.TryGet(payoutMethodId);
                string error = null;

                IClaimDestination mtPelerinDestination = null;
                (mtPelerinDestination, error) = await payoutHandler.ParseAndValidateClaimDestination(MtPelerinSettings.BtcDestAdress, ppBlob, cancellationToken);

                var result = await _pullPaymentHostedService.Claim(new ClaimRequest
                {
                    Destination = mtPelerinDestination,
                    PullPaymentId = ppId,
                    ClaimedAmount = amount,
                    PayoutMethodId = payoutMethodId,
                    StoreId = pp.StoreId
                });

                if (result.Result != ClaimRequest.ClaimResult.Ok)
                {
                    throw new Exception("Error creating Claim");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "MtPelerinPlugin:CreatePayout()");
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
                        var lightningClient = GetLightningClient(store);
                        var balance = await lightningClient.GetBalance();
                        cnfg.OffChainBalance = (balance.OffchainBalance != null
                                               ? (balance.OffchainBalance.Local ?? 0) : 0).ToDecimal(LightMoneyUnit.BTC);
                        try
                        {
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
                _logger.LogError(e, "MtPelerinPlugin:GetBalances()");
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
