using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Events;
using BTCPayServer.HostedServices;
using BTCPayServer.Logging;
using BTCPayServer.Payments;
using BTCPayServer.Plugins.B2PCentral.Models;
using BTCPayServer.Plugins.B2PCentral.Models.P2P;
using BTCPayServer.Plugins.B2PCentral.Models.Swaps;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Tls.Crypto;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static BTCPayServer.Plugins.Monetization.Views.SelectExistingOfferingModalViewModel;



namespace BTCPayServer.Plugins.B2PCentral.Services
{
    public class B2PAutoSwapHostedService : EventHostedServiceBase
    {
        private readonly B2PCentralPluginService _b2pService;
        private readonly ILogger<B2PAutoSwapHostedService> _logger;

        public B2PAutoSwapHostedService(EventAggregator eventAggregator, Logs logs, B2PCentralPluginService b2pService, ILogger<B2PAutoSwapHostedService> logger) : base(eventAggregator, logs)
        {
            _b2pService = b2pService;
            _logger = logger;
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
                        {
                        InvoiceEvent.MarkedCompleted,
                        InvoiceEvent.ReceivedPayment
                    }.Contains(invoiceEvent.Name):
                        var invoice = invoiceEvent.Invoice;
                        var storeSettings = await _b2pService.GetStoreSettings(invoice.StoreId);
                        var payment = invoice.GetPayments(false).Last();

                        var swap = new SwapCreationRequest
                        {
                            QuoteID = string.Empty,
                            IsFixed = true,
                            FromCrypto = "BTC",
                            ToAmount = 0,
                            NotificationEmail = req.NotificationEmail,
                            FromRefundAddress = string.Empty,
                            NotificationNpub = string.Empty
                        };

                        if (payment.PaymentMethodId == PaymentTypes.CHAIN.GetPaymentMethodId("BTC"))
                        {
                            if (!storeSettings.OnChainAutoSwapEnabled)
                                return;
                            if (! await _b2pService.TestPayout())
                                return;
                            var walletBalance = await _b2pService.GetOnChainBalanceInSats(storeSettings.StoreId);
                            if (storeSettings.OnChainAutoSwapThreshold > walletBalance)
                                return;

                            swap.Provider = storeSettings.OnChainAutoSwapProvider;
                            swap.FromNetwork = "Bitcoin";
                            swap.ToCrypto = storeSettings.OnChainAutoSwapCryptoTo;
                            swap.ToNetwork = SwapCryptos.GetNetwork(storeSettings.OnChainAutoSwapCryptoTo);
                            swap.ToAddress = storeSettings.OnChainAutoSwapAddressTo;
                            //swap.FromAmount = (walletBalance * (storeSettings.OnChainAutoSwapPercent / 100)) / 100000000;
                            swap.FromAmount = (walletBalance * storeSettings.OnChainAutoSwapPercent) / 10_000_000_000m;
                        }

                        if (payment.PaymentMethodId == PaymentTypes.LN.GetPaymentMethodId("BTC")
                            || payment.PaymentMethodId == PaymentTypes.LNURL.GetPaymentMethodId("BTC"))
                        {
                            if (!storeSettings.LightningAutoSwapEnabled)
                                return;
                            var walletBalance = await _b2pService.GetLightningBalanceInSats(storeSettings.StoreId);
                            if (storeSettings.LightningAutoSwapThreshold > walletBalance)
                                return;

                            swap.Provider = storeSettings.LightningAutoSwapProvider;
                            swap.FromNetwork = "Lightning";
                            swap.ToCrypto = storeSettings.LightningAutoSwapCryptoTo;
                            swap.ToNetwork = SwapCryptos.GetNetwork(storeSettings.LightningAutoSwapCryptoTo);
                            swap.ToAddress = storeSettings.LightningAutoSwapAddressTo;
                            swap.FromAmount = (walletBalance * storeSettings.LightningAutoSwapPercent) / 10_000_000_000m;
                        }

                        var createdSwap = await _b2pService.CreateSwapAsync(swap, storeSettings.ApiKey);

                        if (createdSwap != null && createdSwap.Success)
                        {
                            var sProvider = swap.Provider.GetDisplayName();
                            var (pullPaymentId, payoutId) = await _b2pService.CreatePayout(storeSettings.StoreId, sProvider, createdSwap, req);
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
                                BTCPayPayoutId = payoutId
                            };
                            await _b2pService.AddSwapInDb(dbSwap);
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
