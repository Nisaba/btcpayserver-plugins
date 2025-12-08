using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Abstractions.Services;
using BTCPayServer.Plugins.MtPelerin.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Plugins.MtPelerin;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.0.1" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("store-wallets-nav", "MtPelerinPluginHeaderNav")       
                .AddHostedService<ApplicationPartsLogger>()
                .AddHostedService<PluginMigrationRunner>()
                .AddSingleton<MtPelerinPluginDbContextFactory>()
                .AddSingleton<MtPelerinPluginService>();

    }

}
