using BTCPayServer.Events;
using BTCPayServer.HostedServices;
using BTCPayServer.Logging;
using BTCPayServer.Payments;
using BTCPayServer.Plugins.B2PCentral.Models;
using BTCPayServer.Plugins.B2PCentral.Models.Swaps;
using BTCPayServer.Services.Stores;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;



namespace BTCPayServer.Plugins.B2PCentral.Services
{
    public class B2PAutoSwapHostedService : EventHostedServiceBase
    {
        private readonly B2PCentralService _b2pService;
        private readonly B2PCentralPluginService _pluginService;
        private readonly ILogger<B2PAutoSwapHostedService> _logger;
        private readonly StoreRepository _storeRepository;

        public B2PAutoSwapHostedService(EventAggregator eventAggregator,
                                        Logs logs, B2PCentralService b2pService,
                                        B2PCentralPluginService pluginService,
                                        ILogger<B2PAutoSwapHostedService> logger,
                                        StoreRepository storeRepository) : base(eventAggregator, logs)
        {
            _b2pService = b2pService;
            _pluginService = pluginService;
            _logger = logger;
            _storeRepository = storeRepository;
        }

        protected override void SubscribeToEvents()
        {
            Subscribe<InvoiceEvent>();
            base.SubscribeToEvents();
        }

        protected override async Task ProcessEvent(object evt, CancellationToken cancellationToken)
        {
            try
            {
                switch (evt)
                {
                    case InvoiceEvent invoiceEvent when new[]
                        { InvoiceEvent.PaymentSettled,
                    }.Contains(invoiceEvent.Name):
                        var invoice = invoiceEvent.Invoice;
                        var storeSettings = await _pluginService.GetStoreSettings(invoice.StoreId);
                        var payment = invoice.GetPayments(false).Last();
                        var store = await _storeRepository.FindStore(invoice.StoreId);

                        var storeUsers = await _storeRepository.GetStoreUsers(invoice.StoreId);
                        var mainUser = storeUsers.First(a => a.StoreRole.Role == "Owner");

                        var swap = new SwapCreationRequest
                        {
                            QuoteID = string.Empty,
                            IsFixed = true,
                            FromCrypto = "BTC",
                            ToAmount = 0,
                            NotificationEmail = mainUser.Email,
                            FromRefundAddress = string.Empty,
                            NotificationNpub = string.Empty
                        };

                        if (payment.PaymentMethodId == PaymentTypes.CHAIN.GetPaymentMethodId("BTC"))
                        {
                            if (!storeSettings.OnChainAutoSwapEnabled)
                                break;
                            if (! await _pluginService.TestPayout())
                                break;
                            var walletBalance = await _pluginService.GetOnChainBalanceInSats(store);
                            if (storeSettings.OnChainAutoSwapThreshold > walletBalance)
                                break;

                            swap.Provider = storeSettings.OnChainAutoSwapProvider;
                            swap.FromNetwork = "Bitcoin";
                            swap.ToCrypto = storeSettings.OnChainAutoSwapCryptoTo.Split("-")[0];
                            swap.ToNetwork = SwapCryptos.GetNetwork(storeSettings.OnChainAutoSwapCryptoTo);
                            swap.ToAddress = storeSettings.OnChainAutoSwapAddressTo;
                            //swap.FromAmount = (walletBalance * (storeSettings.OnChainAutoSwapPercent / 100)) / 100000000;
                            swap.FromAmount = (walletBalance * storeSettings.OnChainAutoSwapPercent) / 10_000_000_000m;
                        } 
                        else if (payment.PaymentMethodId == PaymentTypes.LN.GetPaymentMethodId("BTC")
                            || payment.PaymentMethodId == PaymentTypes.LNURL.GetPaymentMethodId("BTC"))
                        {
                            if (!storeSettings.LightningAutoSwapEnabled)
                                break;
                            var walletBalance = await _pluginService.GetLightningBalanceInSats(store);
                            if (storeSettings.LightningAutoSwapThreshold > walletBalance)
                                break;

                            swap.Provider = storeSettings.LightningAutoSwapProvider;
                            swap.FromNetwork = "Lightning";
                            swap.ToCrypto = storeSettings.LightningAutoSwapCryptoTo.Split("-")[0];
                            swap.ToNetwork = SwapCryptos.GetNetwork(storeSettings.LightningAutoSwapCryptoTo);
                            swap.ToAddress = storeSettings.LightningAutoSwapAddressTo;
                            swap.FromAmount = (walletBalance * storeSettings.LightningAutoSwapPercent) / 10_000_000_000m;
                        }
                        else
                        {
                            break;
                        }

                        var req = new SwapRateRequest
                        {
                            FiatCurrency = string.Empty,
                            FromAmount = swap.FromAmount,
                            FromCrypto = swap.FromCrypto,
                            FromNetwork = swap.FromNetwork,
                            ToAmount = 0,
                            ToCrypto = swap.ToCrypto,
                            ToNetwork = swap.ToNetwork,
                            Providers = [swap.Provider]
                        };
                        var quoteResult = await _b2pService.GetSwapsListAsync(req, storeSettings.ApiKey);
                        swap.QuoteID = quoteResult.First().FixedQuoteId;

                        var createdSwap = await _b2pService.CreateSwapAsync(swap, storeSettings.ApiKey);

                        if (createdSwap != null && createdSwap.Success)
                        {
                            var sProvider = swap.Provider.GetDisplayName();
                            var (pullPaymentId, payoutId) = await _pluginService.CreatePayout(storeSettings.StoreId, sProvider, createdSwap, swap.FromAmount, cancellationToken);
                            var dbSwap = new B2PStoreSwap
                            {
                                StoreId = storeSettings.StoreId,
                                DateT = DateTime.UtcNow,
                                Provider = swap.Provider,
                                SwapId = createdSwap.SwapId,
                                FollowUrl = createdSwap.FollowUrl,
                                ProviderUrl = createdSwap.ProviderUrl,
                                FromAmount = swap.FromAmount,
                                ToAmount = swap.ToAmount,
                                ToCrypto = swap.ToCrypto,
                                ToNetwork = swap.ToNetwork,
                                BTCPayPullPaymentId = pullPaymentId,
                                BTCPayPayoutId = payoutId,
                                IsAutoSwap = true,
                                IsCheckoutSwap = false
                            };
                            await _pluginService.AddSwapInDb(dbSwap);
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B2PCentralPlugin:AutoSwapHostedService()");
            }
            await base.ProcessEvent(evt, cancellationToken);
        }

    }
}
