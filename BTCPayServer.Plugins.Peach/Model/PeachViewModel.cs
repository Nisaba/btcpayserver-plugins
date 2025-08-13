using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Peach.Model
{
    public class PeachViewModel
    {
        public PeachSettings Settings { get; set; }

        [Display(Name = "Your Means of Payment")]
        public List<string> MeansOfPayments { get; set; }

        public bool IsPayoutCreated { get; set; } = false;
        public string PeachToken { get; set; }
    }
}
