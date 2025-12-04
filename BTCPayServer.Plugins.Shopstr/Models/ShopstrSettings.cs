using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;

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

    }
}
