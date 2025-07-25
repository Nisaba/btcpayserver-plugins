using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Exolix.Model
{
    public class SwapMerchantRequest
    {
        public string ToCrypto { get; set; }

        public string ToAddress { get; set; }

        public float BtcAmount { get; set; }

    }
}
