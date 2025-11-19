using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Plugins.Shopstr.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Shopstr;

public class PluginMigrationRunner(ISettingsRepository settingsRepository,
        ShopstrDbContextFactory pluginDbContextFactory) : IHostedService
{
    private readonly ShopstrDbContextFactory _pluginDbContextFactory = pluginDbContextFactory;
    private readonly ISettingsRepository _settingsRepository = settingsRepository;


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

