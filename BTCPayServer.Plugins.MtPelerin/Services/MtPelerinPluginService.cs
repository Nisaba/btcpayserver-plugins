using BTCPayServer.Client.Models;
using BTCPayServer.Configuration;
using BTCPayServer.Data;
using BTCPayServer.Lightning;
using BTCPayServer.Models.StoreViewModels;
using BTCPayServer.Payments;
using BTCPayServer.Payments.Bitcoin;
using BTCPayServer.Payments.Lightning;
using BTCPayServer.Plugins.MtPelerin.Model;
using BTCPayServer.Services;
using BTCPayServer.Services.Invoices;
using BTCPayServer.Services.Stores;
using BTCPayServer.Services.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NBitcoin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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

        public MtPelerinPluginService(MtPelerinPluginDbContextFactory pluginDbContextFactory,
                                      StoreRepository storeRepository,
                                      BTCPayNetworkProvider networkProvider,
                                      BTCPayWalletProvider walletProvider,
                                      WalletHistogramService walletHistogramService,
                                      ILogger<MtPelerinPluginService> logger,
                                      HttpClient httpClient2,
                                      LightningClientFactoryService lightningClientFactory,
                                      IOptions<LightningNetworkOptions> lightningNetworkOptions,
                                      PaymentMethodHandlerDictionary handlers)
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
        }

        public async Task<MtPelerinSettings> GetStoreSettings(string storeId)
        {
            try
            {
                var settings = await _context.MtPelerinSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                if (settings == null)
                {
                    settings = new MtPelerinSettings { StoreId = storeId, ApiKey = string.Empty, Lang = "en", Phone = string.Empty };
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
                    dbSettings.ApiKey = settings.ApiKey;
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

        public async Task<List<MtPelerinTx>> GetStoreTransactions(string storeId)
        {
            try
            {
                var txs = await _context.MtPelerinTransactions.Where(a => a.StoreId == storeId).ToListAsync();
                return txs.Reverse<MtPelerinTx>().ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "MtPelerinPlugin:GetStoreTransactions()");
                throw;
            }
        }

        public async Task AddStoreTransaction (MtPelerinTx tx)
        {
            try
            {
                await _context.MtPelerinTransactions.AddAsync(tx);
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "MtPelerinPlugin:AddStoreTransaction()");
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
