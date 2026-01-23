using System;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.TelegramBot.Models
{
    public class TelegramBotInvoices
    {
        [Key]
        public string BTCPayInvoiceId { get; set; }

        public string StoreId { get; set; }
        public string AppName { get; set; }

        public DateTime DateT { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }

    }
}
