using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Plugins.Ecwid.Data;
using BTCPayServer.Plugins.Ecwid.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Plugins.Ecwid;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.0.1" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("header-nav", "EcwidPluginHeaderNav")
            .AddHostedService<ApplicationPartsLogger>()
            .AddHostedService<PluginMigrationRunner>()
            .AddSingleton<EcwidPluginService>()
            .AddSingleton<EcwidPluginDbContextFactory>()
            .AddSingleton<EcwidHostedService>()
            .AddHostedService<EcwidHostedService>()
    }
}
