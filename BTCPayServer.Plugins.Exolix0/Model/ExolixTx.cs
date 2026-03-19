
using System;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Exolix.Model
{
    public class ExolixTx
    {
        [Key]
        public string TxID { get; set; }

        public string StoreId { get; set; }

        public string AltcoinFrom { get; set; }
        public float BTCAmount { get; set; }
        public DateTime DateT { get; set; }

        public string BTCPayInvoiceId { get; set; }
    }
}
