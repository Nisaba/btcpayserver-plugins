using System.Collections.Generic;

namespace BTCPayServer.Plugins.B2PCentral.Models.P2P
{
    public class B2PResult //: BasePagingViewModel
    {
        private decimal _rate;

        public decimal Rate { get { return _rate; } set { _rate = value == 0 ? 1 : value; } }
        public string ErrorMsg { get; set; }

        public List<B2POffer> Offers { get; set; }

        //public override int CurrentPageCount => Offers.Count;

    }
}
