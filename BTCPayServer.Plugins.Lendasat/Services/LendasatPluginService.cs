using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using BTCPayServer.Plugins.Lendasat.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using BTCPayServer.Services.Stores;
using BTCPayServer.Services.Wallets;
using BTCPayServer.Client.Models;
using BTCPayServer.Data;
using System.Net.Http;
using BTCPayServer.Services.Invoices;
using System.Collections.Generic;
using BTCPayServer.Models.StoreViewModels;
using System.Linq;
using BTCPayServer.Payments.Bitcoin;
using NBitcoin;

namespace BTCPayServer.Plugins.Lendasat.Services
{
    public class LendasatPluginService
    {
        private readonly LendasatPluginDbContext _context;
        private readonly ILogger<LendasatPluginService> _logger;
        private readonly StoreRepository _storeRepository;
        private readonly BTCPayNetworkProvider _networkProvider;
        private readonly WalletHistogramService _walletHistogramService;
        private readonly BTCPayWalletProvider _walletProvider;
        private readonly HttpClient _httpClient2;
        private readonly PaymentMethodHandlerDictionary _handlers;

        public LendasatPluginService(LendasatPluginDbContextFactory pluginDbContextFactory,
                                     ILogger<LendasatPluginService> logger,
                                     HttpClient httpClient2,
                                     PaymentMethodHandlerDictionary handlers,
                                     StoreRepository storeRepository,
                                     BTCPayNetworkProvider networkProvider,
                                     BTCPayWalletProvider walletProvider,
                                     WalletHistogramService walletHistogramService)
        {
            _context = pluginDbContextFactory.CreateContext();
            _logger = logger;
            _storeRepository = storeRepository;
            _networkProvider = networkProvider;
            _walletHistogramService = walletHistogramService;
            _walletProvider = walletProvider;
            _httpClient2 = httpClient2;
            _handlers = handlers;
        }

        public async Task<LendasatSettings> GetStoreSettings(string storeId)
        {
            try
            {
                var settings = await _context.LendasatSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                if (settings == null)
                {
                    settings = new LendasatSettings { StoreId = storeId, APIKey = string.Empty };
                }
                return settings;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "LendasatPlugin:GetStoreSettings()");
                throw;
            }
        }

        public async Task UpdateSettings(LendasatSettings settings)
        {
            try
            {
                var dbSettings = await _context.LendasatSettings.FirstOrDefaultAsync(a => a.StoreId == settings.StoreId);
                if (dbSettings == null)
                {
                    _context.LendasatSettings.Add(settings);
                }
                else
                {
                    dbSettings.APIKey = settings.APIKey;
                    _context.LendasatSettings.Update(dbSettings);
                }

                await _context.SaveChangesAsync();
                return;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "LendasatPlugin:UpdateSettings()");
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
                _logger.LogError(e, "LendasatPlugin:GetBalances()");
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
