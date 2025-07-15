using System.Collections.Generic;

namespace BTCPayServer.Plugins.Peach.Model
{
    public class UpdateRequest
    {
        public PeachSettings Settings { get; set; }

        public List<PeachMeanOfPayment> MeansOfPayments { get; set; }

    }
}
