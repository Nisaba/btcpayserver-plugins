using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Abstractions.Services;
using BTCPayServer.Plugins.LnOnchainSwaps.Data;
using BTCPayServer.Plugins.LnOnchainSwaps.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Plugins.LnOnchainSwaps;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.0.1" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("store-wallets-nav", "LnOnchainSwapsPluginHeaderNav");
        
        services.AddHostedService<ApplicationPartsLogger>();
        services.AddHostedService<PluginMigrationRunner>();
        services.AddSingleton<LnOnchainSwapsDbContextFactory>();
        services.AddDbContext<LnOnchainSwapsDbContext>((provider, o) =>
        {
            var factory = provider.GetRequiredService<LnOnchainSwapsDbContextFactory>();
            factory.ConfigureBuilder(o);
        });
        services.AddSingleton<LnOnchainSwapsPluginService>();
        services.AddSingleton<BoltzService>();

    }

}
