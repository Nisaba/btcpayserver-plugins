using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Plugins.Ecwid.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Ecwid;

public class PluginMigrationRunner : IHostedService
{
    private readonly EcwidPluginDbContextFactory _pluginDbContextFactory;
    private readonly EcwidPluginService _pluginService;
    private readonly ISettingsRepository _settingsRepository;

    public PluginMigrationRunner(
        ISettingsRepository settingsRepository,
        EcwidPluginDbContextFactory pluginDbContextFactory,
        EcwidPluginService pluginService)
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

        // test record
        // await _pluginService.AddTestDataRecord();
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

