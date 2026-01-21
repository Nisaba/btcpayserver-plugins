using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.TelegramBot.Services
{
    public class AutoStartService(TelegramBotPluginService telegramBotService) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await telegramBotService.LoadAndStartBots();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
