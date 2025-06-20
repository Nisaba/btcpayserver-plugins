
using System;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.MtPelerin.Model
{
    public class MtPelerinTx
    {
        [Key]
        public string TxID { get; set; }

        public string StoreId { get; set; }

    }
}
