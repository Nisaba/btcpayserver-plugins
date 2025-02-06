using BTCPayServer.Client;
using BTCPayServer.Client.Models;
using BTCPayServer.Plugins.Ecwid.Model;
using BTCPayServer.Plugins.Ecwid.Services;
using BTCPayServer.Services.Invoices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Ecwid.Controllers
{
    [Route("~/plugins/{storeId}/EcwidWebhook")]
    public class EcwidWebhookController(ILogger<EcwidWebhookController> logger,
                                         InvoiceRepository invoiceRepository,
                                         EcwidPluginService ecwidService,
                                         BtcPayService btcPayService) : Controller
    {
        private readonly ILogger<EcwidWebhookController> _logger = logger;
        private readonly InvoiceRepository _invoiceRepository = invoiceRepository;
        private readonly EcwidPluginService _ecwidService = ecwidService;
        private readonly BtcPayService _btcPayService = btcPayService;

        [HttpPost]
        public async Task<IActionResult> Index([FromHeader(Name = "BTCPAY-SIG")] string BtcPaySig)
        {
            try
            {
                string jsonStr = await new StreamReader(Request.Body).ReadToEndAsync();
                var webhookEvent = JsonConvert.DeserializeObject<WebhookInvoiceEvent>(jsonStr);
                var BtcPaySecret = BtcPaySig.Split('=')[1];
                if (webhookEvent is null || webhookEvent?.InvoiceId?.StartsWith("__test__") is true || webhookEvent?.Type == WebhookEventType.InvoiceCreated)
                {
                    return Ok();
                }

                if (webhookEvent?.InvoiceId is null || webhookEvent.Metadata?.TryGetValue("orderId", out var orderIdToken) is not true || orderIdToken.ToString() is not { } orderId)
                {
                    _logger.LogWarning("Missing fields in request");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }

                var storeId = Request.Path.Value.Replace("plugins/", "").Replace("/EcwidWebhook", "").Replace("/", "");
                var settings = await _ecwidService.GetStoreSettings(storeId);
                if (!_btcPayService.CheckSecretKey(settings.WebhookSecret, jsonStr, BtcPaySecret))
                {
                    _logger.LogWarning("Bad secret key");
                    return StatusCode(StatusCodes.Status400BadRequest);
                }

                var invoice = await _invoiceRepository.GetInvoice(webhookEvent.InvoiceId);
                if (invoice == null)
                {
                    return StatusCode(StatusCodes.Status406NotAcceptable);
                }

                string sPaymentStatus;
                switch (invoice.Status)
                {
                    case InvoiceStatus.New:
                    case InvoiceStatus.Processing:
                        sPaymentStatus = "none";
                        break;
                    case InvoiceStatus.Expired:
                    case InvoiceStatus.Invalid:
                        sPaymentStatus = "INCOMPLETE";
                        break;
                    case InvoiceStatus.Settled:
                        sPaymentStatus = "PAID";
                        break;
                    default:
                        return Ok();
                }

                if (sPaymentStatus != "none")
                {
                    await _ecwidService.UpdateOrder(new EcwidWebhookModel
                    {
                        PaymentStatus = sPaymentStatus,
                        StoreId = invoice.Metadata.GetAdditionalData<string>("ecwidStoreId"),
                        TransactionId = invoice.Metadata.GetAdditionalData<string>("ecwidRefTransactionId"),
                        Token = invoice.Metadata.GetAdditionalData<string>("ecwidToken")
                    });
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EcwidPlugin:WebhookController()");
                return BadRequest(ex.Message);
            }
        }

    }
}
