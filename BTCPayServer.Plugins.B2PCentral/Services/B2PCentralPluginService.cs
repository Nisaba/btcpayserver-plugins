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
using BTCPayServer.Plugins.B2PCentral.Data;
using BTCPayServer.Plugins.B2PCentral.Models;
using BTCPayServer.Plugins.B2PCentral.Models.P2P;
using BTCPayServer.Plugins.B2PCentral.Models.Swaps;
using BTCPayServer.Services;
using BTCPayServer.Services.Invoices;
using BTCPayServer.Services.Stores;
using BTCPayServer.Services.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
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

public class B2PCentralPluginService(B2PCentralPluginDbContextFactory pluginDbContextFactory,
                                   ApplicationDbContextFactory btcPayDbContextFactory,
                                   StoreRepository storeRepository,
                                   ILogger<B2PCentralPluginService> logger,
                                   HttpClient httpClient,
                                   BTCPayWalletProvider walletProvider,
                                   PaymentMethodHandlerDictionary paymentMethodHandlerDictionary,
                                   LightningClientFactoryService lightningClientFactory,
                                   IOptions<LightningNetworkOptions> lightningNetworkOptions,
                                   BTCPayNetworkProvider networkProvider,
                                   PayoutMethodHandlerDictionary payoutHandlers,
                                   PullPaymentHostedService pullPaymentHostedService,
                                   WalletHistogramService walletHistogramService)
{
    // public const string BaseApiUrl = "https://localhost:7137/api/";
    public const string BaseApiUrl = "https://api.b2p-central.com/api/";
    private HttpClient _httpClient2 = new HttpClient();

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
            logger.LogError(e, "B2PCentral:TestB2P()");
            return e.Message;
        }
    }

    public async Task<B2PSettings> GetStoreSettings(string storeId)
    {
        try
        {
            using var context = pluginDbContextFactory.CreateContext();
            var settings = await context.B2PSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
            if (settings == null)
            {
                settings = new B2PSettings { StoreId = storeId, ProvidersString = "0" };
            }
            return settings;

        }
        catch (Exception e)
        {
            logger.LogError(e, "B2PCentral:GetStoreSettings()");
            throw;
        }
    }

    public async Task<List<B2PStoreSwap>> GetStoreSwaps(string storeId)
    {
        try
        {
            using var context = pluginDbContextFactory.CreateContext();
            var txs = await context.Swaps.Where(a => a.StoreId == storeId).ToListAsync();
            return txs.Reverse<B2PStoreSwap>().ToList();
        }
        catch (Exception e)
        {
            logger.LogError(e, "B2PCentral:GetStoreSwaps()");
            throw;
        }
    }

    public async Task UpdateSettings(B2PSettings settings)
    {
        try
        {
            using var context = pluginDbContextFactory.CreateContext();
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
            logger.LogError(e, "B2PCentral:UpdateSettings()");
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
                    } else
                    {
                        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(3));
                        var wallet = walletProvider.GetWallet(networkProvider.DefaultNetwork);
                        var derivation = store.GetDerivationSchemeSettings(paymentMethodHandlerDictionary, walletId.CryptoCode);
                        if (derivation is not null)
                        {
                            var network = paymentMethodHandlerDictionary.GetBitcoinHandler(walletId.CryptoCode).Network;
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
                                               ? (balance.OffchainBalance.Local ?? 0):0).ToDecimal(LightMoneyUnit.BTC);
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
            logger.LogError(e, "B2PCentral:GetBalances()");
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
            using (var rep = await httpClient.SendAsync(webRequest))
            {
                rep.EnsureSuccessStatusCode();
                using (var rdr = new StreamReader(await rep.Content.ReadAsStreamAsync()))
                {
                    sRep = await rdr.ReadToEndAsync();
                }
            }
            return JsonConvert.DeserializeObject<List<B2POffer>>(sRep);

        }
        catch (Exception ex) {
            logger.LogError(ex.Message, "B2PCentral:GetOffersListAsync()");
            throw;
        }
    }

    public async Task<List<B2PSwap>> GetSwapsListAsync(SwapRateRequest req, string key)
    {
        try
        {
            var reqJson = JsonConvert.SerializeObject(req, Formatting.None);

            var webRequest = new HttpRequestMessage(HttpMethod.Post, "swaps")
            {
                Content = new StringContent(reqJson, Encoding.UTF8, "application/json"),
            };
            webRequest.Headers.Add("B2P-API-KEY", key);

            string sRep;
            using (var rep = await httpClient.SendAsync(webRequest))
            {
                rep.EnsureSuccessStatusCode();
                using (var rdr = new StreamReader(await rep.Content.ReadAsStreamAsync()))
                {
                    sRep = await rdr.ReadToEndAsync();
                }
            }
            return JsonConvert.DeserializeObject<B2PSwapResponse>(sRep).Swaps;

        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, "B2PCentral:GetSwapsListAsync()");
            throw;
        }
    }

    public async Task<SwapCreationResponse> CreateSwapAsync(string storeId, SwapCreationRequest req, string key)
    {
        try
        {
            var reqJson = JsonConvert.SerializeObject(req, Formatting.None);
            var webRequest = new HttpRequestMessage(HttpMethod.Put, "swaps")
            {
                Content = new StringContent(reqJson, Encoding.UTF8, "application/json"),
            };
            webRequest.Headers.Add("B2P-API-KEY", key);
            string sRep;
            using (var rep = await httpClient.SendAsync(webRequest))
            {
                rep.EnsureSuccessStatusCode();
                using (var rdr = new StreamReader(await rep.Content.ReadAsStreamAsync()))
                {
                    sRep = await rdr.ReadToEndAsync();
                }
            }
            return JsonConvert.DeserializeObject<SwapCreationResponse>(sRep);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, "B2PCentral:CreateSwapAsync()");
            throw;
        }
    }

    public async Task<Tuple<string, string>> CreatePayout(string storeId, string provider, SwapCreationResponse swap, SwapCreationRequestJS req, CancellationToken cancellationToken = default)
    {
        try
        {
            var payoutMethodId = PayoutMethodId.Parse("BTC-CHAIN");

            var ppRequest = new CreatePullPaymentRequest
            {
                Name = $"B2P Central {provider} Swap {swap.SwapId}",
                Description = "",
                Amount = req.FromAmount,
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
            IClaimDestination mtPelerinDestination;

#if DEBUG
            (mtPelerinDestination, error) = await payoutHandler.ParseAndValidateClaimDestination("bcrt1qjmraxy9a7dw7ducjmcmp4mm8zd9850882rq2q2", blob, cancellationToken);
#else
            (mtPelerinDestination, error) = await payoutHandler.ParseAndValidateClaimDestination(swap.FromAddress, blob, cancellationToken);
#endif
            if (mtPelerinDestination == null)
                throw new Exception($"Destination parsing failed: {error ?? "Unknown error"}");


            var result = await pullPaymentHostedService.Claim(new ClaimRequest
            {
                Destination = mtPelerinDestination,
                PullPaymentId = ppId,
                ClaimedAmount = req.FromAmount,
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
            return new Tuple<string, string>(ppId, result.PayoutData.Id);

        }
        catch (Exception e)
        {
            logger.LogError(e, "B2PCentral:CreatePayout()");
            throw;
        }
    }

    public async Task<bool> TestPayout()
    {
        try
        {
            await using var btcPayCtx = btcPayDbContextFactory.CreateContext();
            return !await btcPayCtx.Payouts.AnyAsync(a => a.PayoutMethodId == "BTC-CHAIN"
                                                    && a.State != PayoutState.Cancelled
                                                    && a.State != PayoutState.Completed);
        }
        catch (Exception e)
        {
            logger.LogError(e, "B2PCentral:TestPayout()");
            throw;
        }
    }

    public async Task AddSwapInDb(B2PStoreSwap swap)
    {
        try
        {
            using var context = pluginDbContextFactory.CreateContext();
            context.Swaps.Add(swap);
            await context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "B2PCentral:AddSwapInDb()");
            throw;
        }
    }

    private void getPaymentMethods(BTCPayServer.Data.StoreData store, StoreBlob storeBlob,
        out List<StoreDerivationScheme> derivationSchemes, out List<StoreLightningNode> lightningNodes)
    {
        var excludeFilters = storeBlob.GetExcludedPaymentMethods();
        var derivationByCryptoCode =
            store
                .GetPaymentMethodConfigs<DerivationSchemeSettings>(paymentMethodHandlerDictionary)
                .ToDictionary(c => ((IHasNetwork)paymentMethodHandlerDictionary[c.Key]).Network.CryptoCode, c => c.Value);

        var lightningByCryptoCode = store
            .GetPaymentMethodConfigs(paymentMethodHandlerDictionary)
            .Where(c => c.Value is LightningPaymentMethodConfig)
            .ToDictionary(c => ((IHasNetwork)paymentMethodHandlerDictionary[c.Key]).Network.CryptoCode, c => (LightningPaymentMethodConfig)c.Value);

        derivationSchemes = new List<StoreDerivationScheme>();
        lightningNodes = new List<StoreLightningNode>();

        foreach (var handler in paymentMethodHandlerDictionary)
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
        var existing = store.GetPaymentMethodConfig<LightningPaymentMethodConfig>(id, paymentMethodHandlerDictionary);
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

