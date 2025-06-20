using System.Collections.Generic;

namespace BTCPayServer.Plugins.MtPelerin.Model
{
    public class MtPelerinModel
    {
        public MtPelerinSettings Settings { get; set; }

        public List<MtPelerinTx> Transactions { get; set; }
    }
}
