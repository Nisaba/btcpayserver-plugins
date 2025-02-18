using BTCPayServer.Plugins.Ecwid.Model;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using System;
using BTCPayServer.Events;
using BTCPayServer.HostedServices;
using BTCPayServer.Logging;
using System.Linq;

namespace BTCPayServer.Plugins.Ecwid.Services
{
    public class EcwidHostedService : EventHostedServiceBase
    {
        private readonly EcwidPluginService _ecwidService;
        private readonly ILogger<EcwidHostedService> _logger;

        public EcwidHostedService(EventAggregator eventAggregator, Logs logs, EcwidPluginService ecwidService, ILogger<EcwidHostedService> logger) : base(eventAggregator, logs)
        {
            _ecwidService = ecwidService;
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
                        InvoiceEvent.MarkedInvalid,
                        InvoiceEvent.Expired,
                        InvoiceEvent.Confirmed,
                        InvoiceEvent.Completed
                    }.Contains(invoiceEvent.Name):
                        {
                            var invoice = invoiceEvent.Invoice;
                            var ecwidOrderId = invoice.GetInternalTags("ecwidOrderId").FirstOrDefault();
                            if (ecwidOrderId != null)
                            {
                                string invoiceStatus = invoice.Status.ToString().ToLower();
                                bool? success = invoiceStatus switch
                                {
                                    _ when new[] { "complete", "confirmed", "paid", "settled" }.Contains(invoiceStatus) => true,
                                    _ when new[] { "invalid", "expired" }.Contains(invoiceStatus) => false,
                                    _ => (bool?)null
                                };
                                if (success.HasValue)
                                {
                                    await _ecwidService.UpdateOrder(new EcwidCallbackModel
                                    {
                                        PaymentStatus = success.Value ? "PAID" : "INCOMPLETE",
                                        StoreId = invoice.Metadata.GetAdditionalData<string>("ecwidStoreId"),
                                        TransactionId = invoice.Metadata.GetAdditionalData<string>("ecwidRefTransactionId"),
                                        Token = invoice.Metadata.GetAdditionalData<string>("ecwidToken")
                                    });
                                }
                            }
                            break;
                        }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EcwidPlugin:HostedService()");
            }
            await base.ProcessEvent(evt, cancellationToken);
        }
    }
}
