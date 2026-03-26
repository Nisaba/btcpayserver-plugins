using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Plugins.TelegramBot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.TelegramBot;

public class PluginMigrationRunner(
        ISettingsRepository settingsRepository,
        TelegramBotDbContextFactory pluginDbContextFactory,
        ILogger<PluginMigrationRunner> logger) : IHostedService
{

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await settingsRepository.GetSettingAsync<PluginDataMigrationHistory>() ??
                       new PluginDataMigrationHistory();
            await using var ctx = pluginDbContextFactory.CreateContext();
            await ctx.Database.MigrateAsync(cancellationToken);

            // settings migrations
            if (!settings.UpdatedSomething)
            {
                settings.UpdatedSomething = true;
                await settingsRepository.UpdateSetting(settings);
            }
        } catch (Exception ex) { logger.LogError(ex, "TelegramBotPlugin:MigrationRunner()"); }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private class PluginDataMigrationHistory
    {
        public bool UpdatedSomething { get; set; }
    }
}

