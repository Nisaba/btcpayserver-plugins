using BTCPayServer.Data;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.Shopstr.Models
{
    public class ShopstrViewModel
    {
        public string storeId { get; set; }
        public Nip5StoreSettings Nip5Settings { get; set; }
        public ShopstrSettings ShopstrSettings { get; set; }
        public List<ShopAppStoreItem> ShopAppStoreItems { get; set; }

        public List<AppData> StoreApps { get; set; }
    }
}
