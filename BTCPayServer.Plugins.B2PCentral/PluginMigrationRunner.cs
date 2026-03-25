using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Plugins.B2PCentral.Data;
using BTCPayServer.Plugins.B2PCentral.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.B2PCentral;

public class PluginMigrationRunner(
    ISettingsRepository settingsRepository,
    B2PCentralPluginDbContextFactory pluginDbContextFactory,
    B2PCentralPluginService pluginService) : IHostedService
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

        // test record
        // await pluginService.AddTestDataRecord();
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

