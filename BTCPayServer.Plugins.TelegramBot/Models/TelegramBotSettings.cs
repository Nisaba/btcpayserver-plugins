using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.TelegramBot.Models
{
    [PrimaryKey(nameof(StoreId), nameof(AppId))]
    public class TelegramBotSettings
    {
        [Key]
        public string StoreId { get; set; }

        [Key]
        public string AppId { get; set; }

        public string BotToken { get; set; }

        public bool IsEnabled { get; set; }

    }
}
