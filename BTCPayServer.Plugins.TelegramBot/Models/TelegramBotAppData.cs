using BTCPayServer.Client.Models;
using BTCPayServer.Data;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.TelegramBot.Models
{
    public class TelegramBotAppData : AppData
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string CurrencyCode { get; set; }
        public string FormId { get; set; }
        public decimal DefaultTaxRate { get; set; }
        public string BotToken { get; set; }
        public bool IsEnabled { get; set; }

        public List<AppItem> ShopItems { get; set; }

    }
}
