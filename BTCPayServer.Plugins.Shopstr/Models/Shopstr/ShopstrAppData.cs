using BTCPayServer.Client.Models;
using BTCPayServer.Data;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.Shopstr.Models.Shopstr
{
    public class ShopstrAppData : AppData
    {
        public string CurrencyCode { get; set; }
        public string Location { get; set; }
        public List<AppItem> ShopItems { get; set; }

        public bool FlashSales { get; set; }

        public ConditionEnum Condition { get; set; }

        public DateTimeOffset? ValidDateT { get; set; }

        public string Restrictions { get; set; }
    }
}
