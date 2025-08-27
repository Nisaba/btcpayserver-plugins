
using System;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Exolix.Model
{
    public class ExolixMerchantTx
    {
        [Key]
        public string TxID { get; set; }

        public string StoreId { get; set; }

        public string AltcoinTo { get; set; }
        public float BTCAmount { get; set; }
        public float AltAmount { get; set; }
        public DateTime DateT { get; set; }

        public string BTCPayPullPaymentId { get; set; }
    }
}
