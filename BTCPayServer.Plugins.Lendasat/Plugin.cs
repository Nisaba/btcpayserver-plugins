using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Abstractions.Services;
using BTCPayServer.Plugins.Lendasat.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Plugins.Lendasat;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.0.1" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("store-wallets-nav", "LendasatPluginNav");

        services.AddHostedService<ApplicationPartsLogger>();
        services.AddHostedService<PluginMigrationRunner>();
        services.AddSingleton<LendasatPluginDbContextFactory>();
        services.AddDbContext<LendasatPluginDbContext>((provider, o) =>
        {
            var factory = provider.GetRequiredService<LendasatPluginDbContextFactory>();
            factory.ConfigureBuilder(o);
        });
        services.AddSingleton<LendasatPluginService>();
        services.AddSingleton<LendasatService>();

    }

}
