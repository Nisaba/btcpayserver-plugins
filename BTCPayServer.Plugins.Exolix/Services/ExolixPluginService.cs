using Microsoft.Extensions.Logging;
using BTCPayServer.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BTCPayServer.Plugins.Exolix.Model;
using Microsoft.EntityFrameworkCore;
using BTCPayServer.Lightning;
using Newtonsoft.Json;
using System.Threading;
using BTCPayServer.Services.Stores;
using System.Net.Http;
using BTCPayServer.Services.Wallets;
using BTCPayServer.Services.Invoices;
using BTCPayServer.Client.Models;
using BTCPayServer.Data;
using BTCPayServer.Models.StoreViewModels;
using BTCPayServer.Payments.Bitcoin;
using NBitcoin;
using BTCPayServer.HostedServices;
using BTCPayServer.Payouts;

namespace BTCPayServer.Plugins.Exolix.Services
{
    public class ExolixPluginService
    {
        private readonly ILogger<ExolixPluginService> _logger;
        private readonly ExolixPluginDbContext _context;
        private readonly BTCPayServerClient _client;
        private readonly StoreRepository _storeRepository;
        private readonly HttpClient _httpClient2;
        private readonly WalletHistogramService _walletHistogramService;
        private readonly BTCPayWalletProvider _walletProvider;
        private readonly BTCPayNetworkProvider _networkProvider;
        private readonly PaymentMethodHandlerDictionary _handlers;
        private readonly PullPaymentHostedService _pullPaymentService;
        private readonly ApplicationDbContextFactory _btcPayDbContextFactory;
        private readonly PayoutMethodHandlerDictionary _payoutHandlers;
        private readonly PullPaymentHostedService _pullPaymentHostedService;

        public ExolixPluginService(ExolixPluginDbContextFactory pluginDbContextFactory,
                                      BTCPayNetworkProvider networkProvider,
                                      BTCPayWalletProvider walletProvider,
                                      StoreRepository storeRepository,
                                      HttpClient httpClient2,
                                      WalletHistogramService walletHistogramService,
                                      PaymentMethodHandlerDictionary handlers,
                                       ILogger<ExolixPluginService> logger,
                                       BTCPayServerClient client,
                                      PayoutMethodHandlerDictionary payoutHandlers,
                                      PullPaymentHostedService pullPaymentHostedService,
                                      PullPaymentHostedService pullPaymentService,
                                       ApplicationDbContextFactory btcPayDbContextFactory
                                       )
        {
            _logger = logger;
            _context = pluginDbContextFactory.CreateContext();
            _client = client;
            _httpClient2 = httpClient2;
            _storeRepository = storeRepository;
            _walletHistogramService = walletHistogramService;
            _networkProvider = networkProvider;
            _walletProvider = walletProvider;
            _handlers = handlers;
            _pullPaymentService = pullPaymentService;
            _payoutHandlers = payoutHandlers;
            _pullPaymentHostedService = pullPaymentHostedService;
            _btcPayDbContextFactory = btcPayDbContextFactory;
        }

        public async Task<ExolixSettings> GetStoreSettings(string storeId)
        {
            try
            {
                var settings = await _context.ExolixSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                if (settings == null)
                {
                    settings = new ExolixSettings { StoreId = storeId, Enabled = false, AcceptedCryptos = new List<string>(), 
                                                    IsEmailToCustomer = false, AllowRefundAddress = false };
                }
                return settings;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "ExolixPlugin:GetStoreSettings()");
                throw;
            }
        }

        public async Task UpdateSettings(ExolixSettings settings)
        {
            try
            {
                var dbSettings = await _context.ExolixSettings.FirstOrDefaultAsync(a => a.StoreId == settings.StoreId);
                if (dbSettings == null)
                {
                    _context.ExolixSettings.Add(settings);
                }
                else
                {
                    dbSettings.Enabled = settings.Enabled;
                    dbSettings.AcceptedCryptos = new List<string>(settings.AcceptedCryptos ?? new List<string>());
                    dbSettings.IsEmailToCustomer = settings.IsEmailToCustomer;
                    dbSettings.AllowRefundAddress = settings.AllowRefundAddress;
                    _context.ExolixSettings.Update(dbSettings);
                }

                await _context.SaveChangesAsync();
                return;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "ExolixPlugin:UpdateSettings()");
                throw;
            }
        }

        public async Task<List<ExolixTx>> GetStoreTransactions(string storeId)
        {
            try
            {
                var txs = await _context.ExolixTransactions.Where(a => a.StoreId == storeId).ToListAsync();
                return txs.Reverse<ExolixTx>().ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ExolixPlugin:GetStoreTransactions()");
                throw;
            }
        }

        public async Task<List<ExolixMerchantTx>> GetStoreMerchantTransactions(string storeId)
        {
            try
            {
                var txs = await _context.ExolixMerchantTransactions.Where(a => a.StoreId == storeId).ToListAsync();
                return txs.Reverse<ExolixMerchantTx>().ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ExolixPlugin:GetStoreMerchantTransactions()");
                throw;
            }
        }

        public async Task AddStoreTransaction(ExolixTx tx)
        {
            try
            {
                await _context.ExolixTransactions.AddAsync(tx);
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ExolixPlugin:AddStoreTransaction()");
                throw;
            }
        }

        public async Task AddStoreMerchantTransaction(ExolixMerchantTx tx)
        {
            try
            {
                await _context.ExolixMerchantTransactions.AddAsync(tx);
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ExolixPlugin:AddStoreMerchantTransaction()");
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
                        out var derivationSchemes);

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
                _logger.LogError(e, "ExolixPlugin:GetBalances()");
                //            throw;
            }
            return cnfg;

        }

        private void getPaymentMethods(BTCPayServer.Data.StoreData store, StoreBlob storeBlob,
            out List<StoreDerivationScheme> derivationSchemes)
        {
            derivationSchemes = new List<StoreDerivationScheme>();
            var excludeFilters = storeBlob.GetExcludedPaymentMethods();
            var derivationByCryptoCode =
                store
                    .GetPaymentMethodConfigs<DerivationSchemeSettings>(_handlers)
                    .ToDictionary(c => ((IHasNetwork)_handlers[c.Key]).Network.CryptoCode, c => c.Value);


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


        public async Task<string> CreatePayout(string storeId, string offerId, string btcDest, decimal amount, CancellationToken cancellationToken = default)
        {
            try
            {
                var payoutMethodId = PayoutMethodId.Parse("BTC-CHAIN");

                var ppRequest = new CreatePullPaymentRequest
                {
                    Name = $"Exolix Swap {offerId}",
                    Description = "",
                    Amount = amount,
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
                IClaimDestination exolixDestination;

                (exolixDestination, error) = await payoutHandler.ParseAndValidateClaimDestination(btcDest, blob, cancellationToken);

                if (exolixDestination == null)
                    throw new Exception($"Destination parsing failed: {error ?? "Unknown error"}");

                var result = await _pullPaymentHostedService.Claim(new ClaimRequest
                {
                    Destination = exolixDestination,
                    PullPaymentId = ppId,
                    ClaimedAmount = amount,
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
                    case ClaimRequest.ClaimResult.PaymentMethodNotSupported:
                        throw new Exception("Payment Method Not Supported");
                    case ClaimRequest.ClaimResult.Overdraft:
                        throw new Exception("Pull payment: overdraft");
                }

                return ppId;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ExolixPlugin:CreatePayout()");
                throw;
            }
        }

    }
}
