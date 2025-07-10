using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Abstractions.Services;
using BTCPayServer.Plugins.Peach.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Plugins.Peach;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.0.1" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("store-wallets-nav", "PeachPluginNav");

        services.AddHostedService<ApplicationPartsLogger>();
        services.AddHostedService<PluginMigrationRunner>();
        services.AddSingleton<PeachPluginDbContextFactory>();
        services.AddDbContext<PeachPluginDbContext>((provider, o) =>
        {
            var factory = provider.GetRequiredService<PeachPluginDbContextFactory>();
            factory.ConfigureBuilder(o);
        });
        services.AddSingleton<PeachPluginService>();
        services.AddSingleton<PeachService>();

    }

}
