using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Peach.Model
{
    [PrimaryKey(nameof(StoreId), nameof(MoP))]
    public class PeachMeanOfPayment
    {
        public string StoreId { get; set; }

        public string MoP{ get; set; }

        public string HashPaymentData { get; set; }

    }
}
