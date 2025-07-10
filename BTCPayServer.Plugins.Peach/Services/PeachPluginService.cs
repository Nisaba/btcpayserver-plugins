using BTCPayServer.Client.Models;
using BTCPayServer.Configuration;
using BTCPayServer.Data;
using BTCPayServer.HostedServices;
using BTCPayServer.Lightning;
using BTCPayServer.Models.StoreViewModels;
using BTCPayServer.Payments;
using BTCPayServer.Payments.Bitcoin;
using BTCPayServer.Payouts;
using BTCPayServer.Plugins.Peach.Model;
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
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Peach.Services
{
    public class PeachPluginService
    {
        private readonly ILogger<PeachPluginService> _logger;
        private readonly StoreRepository _storeRepository;
        private readonly PeachPluginDbContext _context;
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
        private readonly ExplorerClientProvider _explorerClientProvider;

        public PeachPluginService(PeachPluginDbContextFactory pluginDbContextFactory,
                                      StoreRepository storeRepository,
                                      BTCPayNetworkProvider networkProvider,
                                      BTCPayWalletProvider walletProvider,
                                      WalletHistogramService walletHistogramService,
                                      ILogger<PeachPluginService> logger,
                                      HttpClient httpClient2,
                                      LightningClientFactoryService lightningClientFactory,
                                      IOptions<LightningNetworkOptions> lightningNetworkOptions,
                                      PullPaymentHostedService pullPaymentService,
                                      PaymentMethodHandlerDictionary handlers,
                                      ApplicationDbContextFactory btcPayDbContextFactory,
                                      PayoutMethodHandlerDictionary payoutHandlers,
                                      PullPaymentHostedService pullPaymentHostedService,
                                      ExplorerClientProvider explorerClientProvider)
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
            _explorerClientProvider = explorerClientProvider;
        }

        public async Task<PeachSettings> GetStoreSettings(string storeId)
        {
            try
            {
                var settings = await _context.PeachSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                if (settings == null)
                {
                    settings = new PeachSettings { StoreId = storeId, Lang = "en", Phone = string.Empty, UseBridgeApp = false };
                }
                return settings;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "PeachPlugin:GetStoreSettings()");
                throw;
            }
        }

        public async Task UpdateSettings(PeachSettings settings)
        {
            try
            {
                var dbSettings = await _context.PeachSettings.FirstOrDefaultAsync(a => a.StoreId == settings.StoreId);
                if (dbSettings == null)
                {
                    _context.PeachSettings.Add(settings);
                }
                else
                {
                    dbSettings.Lang = settings.Lang;
                    dbSettings.UseBridgeApp = settings.UseBridgeApp;
                    dbSettings.Phone = settings.Phone;
                    _context.PeachSettings.Update(dbSettings);
                }

                await _context.SaveChangesAsync();
                return;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "PeachPlugin:UpdateSettings()");
                throw;
            }
        }
        public async Task CreatePayout(string storeId, PeachRequest operation, CancellationToken cancellationToken = default)
        {
            try
            {
                var payoutMethodId = operation.IsOnChain ?
                                        PayoutMethodId.Parse("BTC-CHAIN") :
                                        PayoutMethodId.Parse("BTC-LN");

                var ppRequest = new CreatePullPaymentRequest
                {
                    Name = $"Mt Pelerin {operation.Type} {operation.PeachId}",
                    Description = "",
                    Amount = operation.Amount,
                    Currency = "BTC",
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                    PayoutMethods = new[] { payoutMethodId.ToString() }
                };

                var store = await _storeRepository.FindStore(storeId);
                var ppId = await _pullPaymentService.CreatePullPayment(store, ppRequest);

                await using var btcPayCtx = _btcPayDbContextFactory.CreateContext();
                var pp = await btcPayCtx.PullPayments.FindAsync(ppId);
                var blob = pp.GetBlob();

                var payoutHandler = _payoutHandlers.TryGet(payoutMethodId);
                if (payoutHandler == null)
                    throw new Exception($"No payout handler found for {payoutMethodId}");

                string error = null;
                var sDest = operation.IsOnChain
                    ? PeachSettings.BtcDestAdress
                    : operation.LnInvoice;
                IClaimDestination PeachDestination;

                (PeachDestination, error) = await payoutHandler.ParseAndValidateClaimDestination(sDest, blob, cancellationToken);

                if (PeachDestination == null)
                    throw new Exception($"Destination parsing failed: {error ?? "Unknown error"}");

                (PeachDestination, error) = await payoutHandler.ParseAndValidateClaimDestination(sDest, blob, cancellationToken);

                var result = await _pullPaymentHostedService.Claim(new ClaimRequest
                {
                    Destination = PeachDestination,
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
                _logger.LogError(e, "PeachPlugin:CreatePayout()");
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
                    getPaymentMethods(store, blob, out var derivationSchemes);

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

                    if (cnfg.OnChainBalance > 0)
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

                        cnfg.OnChainFiatBalance = cnfg.Rate * cnfg.OnChainBalance;
                    }

                }
                else
                {
                    cnfg.OnChainEnabled = false;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PeachPlugin:GetBalances()");
                //            throw;
            }
            return cnfg;

        }

        private void getPaymentMethods(BTCPayServer.Data.StoreData store, StoreBlob storeBlob,
            out List<StoreDerivationScheme> derivationSchemes)
        {
            var excludeFilters = storeBlob.GetExcludedPaymentMethods();
            var derivationByCryptoCode =
                store
                    .GetPaymentMethodConfigs<DerivationSchemeSettings>(_handlers)
                    .ToDictionary(c => ((IHasNetwork)_handlers[c.Key]).Network.CryptoCode, c => c.Value);

            derivationSchemes = new List<StoreDerivationScheme>();

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
            }
        }
    }
}
