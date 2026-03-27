using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Plugins.Peach.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Peach;

public class PluginMigrationRunner(
        ISettingsRepository settingsRepository,
        PeachPluginDbContextFactory pluginDbContextFactory) : IHostedService
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
        } catch { }
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

