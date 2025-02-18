using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
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
        services.AddUIExtension("header-nav", "EcwidPluginHeaderNav");
        services.AddHostedService<ApplicationPartsLogger>();
        services.AddHostedService<PluginMigrationRunner>();
        services.AddSingleton<EcwidPluginService>();
        services.AddSingleton<BtcPayService>();
        services.AddSingleton<EcwidPluginDbContextFactory>();
        services.AddDbContext<EcwidPluginDbContext>((provider, o) =>
        {
            var factory = provider.GetRequiredService<EcwidPluginDbContextFactory>();
            factory.ConfigureBuilder(o);
        });
        services.AddSingleton<EcwidHostedService>();
        services.AddHostedService<EcwidHostedService>();
    }
}
