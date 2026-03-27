using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Plugins.LnOnchainSwaps.Data;
using BTCPayServer.Plugins.LnOnchainSwaps.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BTCPayServer.Plugins.LnOnchainSwaps;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new() { Identifier = nameof(BTCPayServer), Condition = ">=2.3.7" },
#if BOLTZ_SUPPORT
        new() { Identifier = "BTCPayServer.Plugins.Boltz", Condition = ">=2.2.17" }
#endif
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("store-wallets-nav", "LnOnchainSwapsPluginHeaderNav")
                .AddSingleton<LnOnchainSwapsDbContextFactory>()
                .AddHostedService<PluginMigrationRunner>()
                .AddSingleton<LnOnchainSwapsPluginService>()
                .AddHttpClient<BoltzHttpService>(client =>
                {
                    client.BaseAddress = new Uri(BoltzHttpService.BaseUrl);
                });

    }

}
