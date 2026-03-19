using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Plugins.MtPelerin.Data;
using BTCPayServer.Plugins.MtPelerin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Plugins.MtPelerin;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.3.7" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("store-wallets-nav", "MtPelerinPluginHeaderNav")       
                .AddHostedService<PluginMigrationRunner>()
                .AddSingleton<MtPelerinPluginDbContextFactory>()
                .AddSingleton<MtPelerinPluginService>();

    }

}
