using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace BTCPayServer.Plugins.TelegramBot.Services
{
    public class TelegramBotService(ILogger<TelegramBotService> logger)
    {

        private ITelegramBotClient? _bot;

        public void StartBot(string BotToken, CancellationToken cancellationToken)
        {
            _bot = new TelegramBotClient(BotToken);
            var receiverOptions = new Telegram.Bot.Polling.ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };
            _bot.StartReceiving(
                HandleUpdate,
                HandleError,
                receiverOptions,
                cancellationToken
            );
        }

        private async Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            if (update.Type == UpdateType.Message && update.Message!.Text != null)
            {
                var chatId = update.Message.Chat.Id;
                var text = update.Message.Text;

                await bot.SendMessage(chatId, $"Tu as dit : {text}");
            }
        }

        private Task HandleError(ITelegramBotClient bot, Exception ex, CancellationToken token)
        {
            logger.LogError(ex, $"Bot Telegram: {bot.GetMyName()}");
            return Task.CompletedTask;
        }
    }
}
