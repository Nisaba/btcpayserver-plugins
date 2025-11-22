using BTCPayServer.Data;
using System.Collections.Generic;
using System.Linq;

namespace BTCPayServer.Plugins.Shopstr.Models
{
    public class ShopstrViewModel
    {
        public string storeId { get; set; }
        public Nip5StoreSettings Nip5Settings { get; set; }
        public ShopstrSettings ShopstrSettings { get; set; }
        public List<ShopAppStoreItem> SentItemsToShopstr{ get; set; }
        public List<ShopstrAppData> StoreApps { get; set; }


    }
}
