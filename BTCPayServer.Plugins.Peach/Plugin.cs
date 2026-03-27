using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Plugins.Peach.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BTCPayServer.Plugins.Peach;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.3.7" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("store-wallets-nav", "PeachPluginNav")
                .AddHostedService<PluginMigrationRunner>()
                .AddSingleton<PeachPluginDbContextFactory>()
                .AddSingleton<PeachPluginService>()
                .AddHttpClient<PeachService>(client =>
                {
                    client.BaseAddress = new Uri(PeachService.BaseUrl);
                });

    }

}
