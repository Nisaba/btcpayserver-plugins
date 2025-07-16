using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.Peach.Model
{
    public class PeachPostOfferRequest
    {
        public string PeachToken { get; set; }
        public decimal Amount { get; set; }
        public decimal Premium { get; set; }
        public string CurrencyCode { get; set; }
        public List<PeachMeanOfPayment> MeansOfPayment { get; set; }
        public string ReturnAdress { get; set; }
    }
}
