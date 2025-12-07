using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Abstractions.Services;
using BTCPayServer.Plugins.B2PCentral.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Plugins.B2PCentral;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.0.3" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("header-nav", "B2PCentralPluginHeaderNav")
                .AddHostedService<ApplicationPartsLogger>()
                .AddHostedService<PluginMigrationRunner>()
                .AddSingleton<B2PCentralPluginService>()
                .AddSingleton<B2PCentralPluginDbContextFactory>();
    }
}
