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

        public ExolixPluginService(ExolixPluginDbContextFactory pluginDbContextFactory,
                                      BTCPayNetworkProvider networkProvider,
                                      BTCPayWalletProvider walletProvider,
                                      StoreRepository storeRepository,
                                      HttpClient httpClient2,
                                      WalletHistogramService walletHistogramService,
                                      PaymentMethodHandlerDictionary handlers,
                                   ILogger<ExolixPluginService> logger,
                                   BTCPayServerClient client)
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

        public async Task AddStoreTransaction (ExolixTx tx)
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
    }
}
