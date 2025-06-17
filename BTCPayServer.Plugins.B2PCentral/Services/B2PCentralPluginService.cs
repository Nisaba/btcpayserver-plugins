using BTCPayServer.Client.Models;
using BTCPayServer.Configuration;
using BTCPayServer.Data;
using BTCPayServer.Lightning;
using BTCPayServer.Models.StoreViewModels;
using BTCPayServer.Payments;
using BTCPayServer.Payments.Bitcoin;
using BTCPayServer.Payments.Lightning;
using BTCPayServer.Plugins.B2PCentral.Data;
using BTCPayServer.Plugins.B2PCentral.Models;
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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.B2PCentral.Services;

public class B2PCentralPluginService
{
    private readonly B2PCentralPluginDbContextFactory _pluginDbContextFactory;
    private readonly StoreRepository _storeRepository;
    private readonly ILogger _logger;
    private readonly B2PCentralPluginDbContext context;
    private readonly HttpClient _httpClient;
    private readonly HttpClient _httpClient2;
    private readonly BTCPayNetworkProvider _networkProvider;
    private readonly PaymentMethodHandlerDictionary _paymentMethodHandlerDictionary;
    private readonly BTCPayWalletProvider _walletProvider;
    private readonly LightningClientFactoryService _lightningClientFactory;
    private readonly IOptions<LightningNetworkOptions> _lightningNetworkOptions;
    private readonly WalletHistogramService _walletHistogramService;
    private readonly PaymentMethodHandlerDictionary _handlers;

    public B2PCentralPluginService(B2PCentralPluginDbContextFactory pluginDbContextFactory,
                                   StoreRepository storeRepository,
                                   ILogger<B2PCentralPluginService> logger,
                                   HttpClient httpClient,
                                   HttpClient httpClient2,
                                   BTCPayWalletProvider walletProvider,
                                   PaymentMethodHandlerDictionary paymentMethodHandlerDictionary,
                                   LightningClientFactoryService lightningClientFactory,
                                   IOptions<LightningNetworkOptions> lightningNetworkOptions,
                                   BTCPayNetworkProvider networkProvider,
                                   WalletHistogramService walletHistogramService,
                                   PaymentMethodHandlerDictionary handlers)
    {
        _pluginDbContextFactory = pluginDbContextFactory;
        _storeRepository = storeRepository;
        _logger = logger;
        context = _pluginDbContextFactory.CreateContext();
        _networkProvider = networkProvider;
        _paymentMethodHandlerDictionary = paymentMethodHandlerDictionary;
        _walletProvider = walletProvider;

        _lightningClientFactory = lightningClientFactory;
        _lightningNetworkOptions = lightningNetworkOptions;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.b2p-central.com/api/");
        _httpClient2 = httpClient2;
        _walletHistogramService = walletHistogramService;
        _handlers = handlers;
    }

    public async Task<string> TestB2P(B2PSettings settings)
    {
        try
        {
            await GetOffersListAsync(new OffersRequest
                                    {
                                        IsBuy = true,
                                        CurrencyCode = "EUR",
                                        Amount = 0,
                                        Providers = new[] {ProvidersEnum.None}
                                    }, settings.ApiKey);
            return "OK";
        }
        catch (Exception e)
        {
            _logger.LogError(e, "B2PCentral:TestB2P()");
            return e.Message;
        }
    }

    public async Task<B2PSettings> GetStoreSettings(string storeId)
    {
        try
        {
            var settings = await context.B2PSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
            if (settings == null)
            {
                settings = new B2PSettings { StoreId = storeId, ProvidersString = "0" };
            }
            return settings;

        }
        catch (Exception e)
        {
            _logger.LogError(e, "B2PCentral:GetStoreSettings()");
            throw;
        }
    }

    public async Task UpdateSettings(B2PSettings settings)
    {
        try
        {
            var dbSettings = await context.B2PSettings.FirstOrDefaultAsync(a => a.StoreId == settings.StoreId);
            if (dbSettings == null)
            {
                context.B2PSettings.Add(settings);
            } else
            {
                dbSettings.ProvidersString = settings.ProvidersString;
                dbSettings.ApiKey = settings.ApiKey;
                context.B2PSettings.Update(dbSettings);
            }

            await context.SaveChangesAsync();
            return;

        }
        catch (Exception e)
        {
            _logger.LogError(e, "B2PCentral:UpdateSettings()");
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
                    } else
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
                                           ? (balance.OffchainBalance.Local ?? 0):0).ToDecimal(LightMoneyUnit.BTC);
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

                if (cnfg.OnChainBalance > 0 || cnfg.OffChainBalance > 0) {
                    if (_httpClient2.BaseAddress ==  null)
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

            } else
            {
                cnfg.OffChainEnabled = false;
                cnfg.OnChainEnabled = false;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "B2PCentral:GetBalances()");
//            throw;
        }
        return cnfg;

    }

    public async Task<List<B2POffer>> GetOffersListAsync(OffersRequest req, string key)
    {
        try
        {
            var reqJson = JsonConvert.SerializeObject(req, Formatting.None);
            
            var webRequest = new HttpRequestMessage(HttpMethod.Post, "Offers")
            {
                Content = new StringContent(reqJson, Encoding.UTF8, "application/json"),
            };
            webRequest.Headers.Add("B2P-API-KEY", key);

            string sRep;
            using (var rep = await _httpClient.SendAsync(webRequest))
            {
                rep.EnsureSuccessStatusCode();
                using (var rdr = new StreamReader(await rep.Content.ReadAsStreamAsync()))
                {
                    sRep = await rdr.ReadToEndAsync();
                }
            }
            return JsonConvert.DeserializeObject<List<B2POffer>>(sRep);

        }
        catch { throw;  }
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

