using BTCPayServer.Client.Models;
using BTCPayServer.Data;
using BTCPayServer.Events;
using BTCPayServer.Payments;
using BTCPayServer.Services.Invoices;
using NBitcoin;

namespace BTCPayServer.Plugins.Satora.Services;

// Settles a BTCPay invoice once a Satora swap's BTC has been claimed
// into the plugin's store wallet.
//
// We deliberately do NOT route the swapped BTC to the invoice's own
// Arkade address — it stays in the SDK's internal (store-seed) wallet —
// so ArkPayServer's listener never observes it and cannot auto-settle.
// Instead we register the settlement directly against the invoice using
// the Arkade claim txid, mirroring exactly what ArkPayServer's own
// listener does (PaymentData + PaymentService.AddPayment). The invoice
// ledger then shows a real Arkade payment (amount + method + txid)
// rather than a bare "marked settled".
//
// No compile-time dependency on the ArkPayServer assembly: the payment
// method id is referenced by string ("ARKADE") and the payment details
// blob is an anonymous object whose property names match ArkPayServer's
// ArkadePaymentData(Outpoint, Destination) record, so it round-trips
// through the handler's serializer.
public class SatoraSettlementService(
    PaymentService paymentService,
    InvoiceRepository invoiceRepository,
    PaymentMethodHandlerDictionary handlers,
    EventAggregator eventAggregator,
    ILogger<SatoraSettlementService> logger)
{
    private static readonly PaymentMethodId ArkadePmi = PaymentMethodId.Parse("ARKADE");

    public async Task SettleAsync(string invoiceId, string arkTxid, ulong claimSats)
    {
        if (string.IsNullOrEmpty(invoiceId))
        {
            logger.LogWarning("SatoraSettlement: no invoice id, nothing to settle");
            return;
        }

        var invoice = await invoiceRepository.GetInvoice(invoiceId);
        if (invoice is null)
        {
            logger.LogWarning("SatoraSettlement: invoice {InvoiceId} not found", invoiceId);
            return;
        }

        // Payment id keyed on the claim txid so re-running is idempotent
        // (AddPayment returns null on a duplicate). The Arkade VHTLC claim
        // produces a single output, so index 0.
        var outpoint = $"{arkTxid}:0";

        // ArkPayServer not installed, or this invoice has no Arkade prompt
        // to attach the payment to → fall back to a plain status mark so
        // the order still settles, just without a payment ledger entry.
        if (!handlers.TryGetValue(ArkadePmi, out var handler) || invoice.GetPaymentPrompt(ArkadePmi) is null)
        {
            logger.LogWarning("SatoraSettlement: no ARKADE handler/prompt for invoice {InvoiceId}; marking settled without a payment record", invoiceId);
            await invoiceRepository.MarkInvoiceStatus(invoiceId, InvoiceStatus.Settled);
            return;
        }

        var details = new { Outpoint = outpoint, Destination = (string?)null };
        var paymentData = new PaymentData
        {
            Status = PaymentStatus.Settled,
            Amount = Money.Satoshis((long)claimSats).ToDecimal(MoneyUnit.BTC),
            Created = DateTimeOffset.UtcNow,
            Id = outpoint,
            Currency = "BTC",
        }.Set(invoice, handler, details);

        var payment = await paymentService.AddPayment(paymentData);
        if (payment is null)
        {
            // Already registered on a prior call — idempotent no-op.
            logger.LogInformation("SatoraSettlement: payment {Outpoint} already registered for invoice {InvoiceId}", outpoint, invoiceId);
            return;
        }

        // Nudge the invoice watcher to re-evaluate so it flips to Settled
        // once payments cover the due (mirrors ArkPayServer).
        eventAggregator.Publish(new InvoiceNeedUpdateEvent(invoice.Id));
        logger.LogInformation("SatoraSettlement: registered {Sats} sats Arkade payment (txid {Txid}) for invoice {InvoiceId}", claimSats, arkTxid, invoiceId);
    }
}
