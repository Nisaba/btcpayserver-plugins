using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Exolix.Model
{
    public class SwapCreationRequest
    {
        public string FromCrypto { get; set; }

        public string FromNetwork { get; set; }

        public string ToCrypto { get; set; }

        public string ToNetwork { get; set; }

        public float FromAmount { get; set; }

        public float ToAmount { get; set; }

        public string ToAddress { get; set; }

        public string FromRefundAddress { get; set; }

    }
}
