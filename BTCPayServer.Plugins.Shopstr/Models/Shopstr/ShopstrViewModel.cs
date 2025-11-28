using BTCPayServer.Data;
using BTCPayServer.Plugins.Shopstr.Models.External;
using System.Collections.Generic;
using System.Linq;

namespace BTCPayServer.Plugins.Shopstr.Models.Shopstr
{
    public class ShopstrViewModel
    {
        public string storeId { get; set; }
        public Nip5StoreSettings Nip5Settings { get; set; }
        public List<ShopstrAppData> StoreApps { get; set; }

    }
}
