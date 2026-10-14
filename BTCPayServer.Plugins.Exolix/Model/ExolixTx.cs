using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        
        [StringLength(64)]
        public string ReadTokenHash { get; set; }
        
        [NotMapped]
        public string ReadToken { get; set; }
    }
}
