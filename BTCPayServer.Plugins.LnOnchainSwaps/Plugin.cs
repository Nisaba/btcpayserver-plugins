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
        new() { Identifier = nameof(BTCPayServer), Condition = ">=2.0.1" },
#if BOLTZ_SUPPORT
        new() { Identifier = "BTCPayServer.Plugins.Boltz", Condition = ">=2.2.17" }
#endif
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("store-wallets-nav", "LnOnchainSwapsPluginHeaderNav")
                .AddHostedService<ApplicationPartsLogger>()
                .AddSingleton<LnOnchainSwapsDbContextFactory>()
                .AddHostedService<PluginMigrationRunner>()
                .AddSingleton<LnOnchainSwapsPluginService>()
                .AddSingleton<BoltzHttpService>();

    }

}
