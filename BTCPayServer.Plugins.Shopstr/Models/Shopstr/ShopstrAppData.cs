using BTCPayServer.Client.Models;
using BTCPayServer.Data;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using System.IO;

namespace BTCPayServer.Plugins.Shopstr.Models.Shopstr
{
    public class ShopstrAppData : AppData
    {
        public string CurrencyCode { get; set; }
        public string Location { get; set; }
        public List<AppItem> ShopItems { get; set; }

    }
}
