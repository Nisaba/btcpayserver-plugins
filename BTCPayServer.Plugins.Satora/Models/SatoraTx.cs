
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Satora.Models
{
    public class SatoraTx
    {
        [Key]
        public string TxID { get; set; }

        public string Status { get; set; }
        public string StoreId { get; set; }

        public string Stablecoin { get; set; }
        public string Blockchain { get; set; }
        public float BTCAmount { get; set; }
        public DateTime DateT { get; set; }

        public string BTCPayInvoiceId { get; set; }
    }
}
