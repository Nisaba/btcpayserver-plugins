using System.Collections.Generic;

namespace BTCPayServer.Plugins.Exolix.Model
{
    public class ExolixModel
    {
        public ExolixSettings Settings { get; set; }

        public List<ExolixTx> Transactions { get; set; }
        public List<ExolixMerchantTx> MerchantTransactions { get; set; }
    }
}
