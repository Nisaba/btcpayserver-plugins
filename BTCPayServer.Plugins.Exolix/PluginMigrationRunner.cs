using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Plugins.Exolix.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Exolix;

public class PluginMigrationRunner : IHostedService
{
    private readonly ExolixPluginDbContextFactory _pluginDbContextFactory;
    private readonly ExolixService _pluginService;
    private readonly ISettingsRepository _settingsRepository;

    public PluginMigrationRunner(
        ISettingsRepository settingsRepository,
        ExolixPluginDbContextFactory pluginDbContextFactory,
        ExolixService pluginService)
    {
        _settingsRepository = settingsRepository;
        _pluginDbContextFactory = pluginDbContextFactory;
        _pluginService = pluginService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
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

