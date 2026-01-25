using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.TelegramBot.Models
{
    public class TelegramBotConfig
    {
        [Key]
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }

    }
}
