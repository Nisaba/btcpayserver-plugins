using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Shopstr.Models
{
    [PrimaryKey(nameof(StoreId), nameof(AppId))]
    public class ShopstrSettings
    {
        [Key]
        public string StoreId { get; set; }
        [Key]
        public string AppId { get; set; }

        public string Location { get; set; }

        public bool FlashSales { get; set; }

        public ConditionEnum Condition { get; set; }

        public DateTimeOffset? ValidDateT { get; set; }

        public string Restrictions { get; set; }

    }

    public enum ConditionEnum
    {
        [Description("")]
        None,

        [Description("New")]
        New,

        [Description("Renewed")]
        Renewed,

        [Description("Used - Like New")]
        UsedLikeNew,

        [Description("Used - Very Good")]
        UsedVeryGood,

        [Description("Used - Good")]
        UsedGood,

        [Description("Used - Acceptable")]
        UsedAcceptable
    }

}
