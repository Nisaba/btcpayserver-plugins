using BTCPayServer.Data;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.TelegramBot.Models
{
    public class TelegramBotViewModel
    {
        public string storeId { get; set; }
        public List<TelegramBotAppData> StoreApps { get; set; }
    }
}
