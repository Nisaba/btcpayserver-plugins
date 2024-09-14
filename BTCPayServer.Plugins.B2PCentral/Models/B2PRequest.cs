using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.B2PCentral.Models
{
    public struct B2PRequest
    {
        public decimal Rate { get; set; }
        public string ApiKey { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Amount { get; set; }
        public ProvidersEnum[] Providers { get; set; }


    }
}
