using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Plugins.MtPelerin.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.MtPelerin;

public class PluginMigrationRunner : IHostedService
{
    private readonly MtPelerinPluginDbContextFactory _pluginDbContextFactory;
    private readonly ISettingsRepository _settingsRepository;

    public PluginMigrationRunner(
        ISettingsRepository settingsRepository,
        MtPelerinPluginDbContextFactory pluginDbContextFactory)
    {
        _settingsRepository = settingsRepository;
        _pluginDbContextFactory = pluginDbContextFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _settingsRepository.GetSettingAsync<PluginDataMigrationHistory>() ??
                       new PluginDataMigrationHistory();
            await using var ctx = _pluginDbContextFactory.CreateContext();
            await ctx.Database.MigrateAsync(cancellationToken);

            // settings migrations
            if (!settings.UpdatedSomething)
            {
                settings.UpdatedSomething = true;
                await _settingsRepository.UpdateSetting(settings);
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

