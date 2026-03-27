using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Plugins.Ecwid.Data;
using BTCPayServer.Plugins.Ecwid.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Ecwid;

public class PluginMigrationRunner(
        ISettingsRepository settingsRepository,
        EcwidPluginDbContextFactory pluginDbContextFactory) : IHostedService
{

    public async Task StartAsync(CancellationToken cancellationToken)
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

