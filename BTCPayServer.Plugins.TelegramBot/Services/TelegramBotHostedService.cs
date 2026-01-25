using BTCPayServer.Plugins.TelegramBot.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using System;
using BTCPayServer.Events;
using BTCPayServer.HostedServices;
using BTCPayServer.Logging;
using System.Linq;

namespace BTCPayServer.Plugins.TelegramBot.Services
{
    public class TelegramBotHostedService : EventHostedServiceBase
    {
        private readonly TelegramBotPluginService _service;
        private readonly ILogger<TelegramBotHostedService> _logger;

        public TelegramBotHostedService(TelegramBotPluginService service, ILogger<TelegramBotHostedService> logger, EventAggregator eventAggregator, Logs logs) : base(eventAggregator, logs)
        {
            _service = service;
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
                            var itemDesc = invoice.Metadata.ItemDesc;
                            if (itemDesc != null && itemDesc.StartsWith("From Telegram Bot"))
                            {
                                var appId = invoice.Metadata.GetAdditionalData<string>("appId");
                                string invoiceStatus = invoice.Status.ToString().ToLower();
                                bool? success = invoiceStatus switch
                                {
                                    _ when new[] { "complete", "confirmed", "paid", "settled" }.Contains(invoiceStatus) => true,
                                    _ when new[] { "invalid", "expired" }.Contains(invoiceStatus) => false,
                                    _ => (bool?)null
                                };
                                if (success.HasValue)
                                {
                                    var bot = _service.telegramBots.Where(a => a.AppData.Id == appId).FirstOrDefault();
                                    var chatId = invoice.Metadata.GetAdditionalData<long>("chatId");
                                    if (success.Value)
                                    {
                                        await bot.SendPaymentSuccess(chatId);
                                    }
                                    else
                                    {
                                        await bot.SendPaymentFailure(chatId);
                                    }
                                }
                            }
                            break;
                        }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TelegramBotPlugin:HostedService()");
            }
            await base.ProcessEvent(evt, cancellationToken);
        }
    }
}
