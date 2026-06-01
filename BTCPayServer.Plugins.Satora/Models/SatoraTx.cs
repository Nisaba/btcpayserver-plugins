
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Satora.Models
{
    public class SatoraTx
    {
        [Key]
        public string TxID { get; set; }

        public string StoreId { get; set; }

        public string? Status { get; set; }

        public string Stablecoin { get; set; }
        public string Blockchain { get; set; }
        public float BTCAmount { get; set; }
        public DateTime DateT { get; set; }

        public string BTCPayInvoiceId { get; set; }

        // Arkade txid of the claim that swept the VHTLC into the store
        // wallet. Recorded so the BTCPay invoice settlement carries a real
        // settlement reference, and so a failed settle can be retried.
        public string? ClaimTxId { get; set; }

    }
}
