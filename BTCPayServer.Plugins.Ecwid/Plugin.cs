using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Plugins.Ecwid.Data;
using BTCPayServer.Plugins.Ecwid.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BTCPayServer.Plugins.Ecwid;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.3.7" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("header-nav", "EcwidPluginHeaderNav")
            .AddHostedService<PluginMigrationRunner>()
            .AddSingleton<EcwidPluginDbContextFactory>()
            .AddHttpClient<EcwidPluginService>();

         services.AddSingleton<EcwidHostedService>()
            .AddHostedService(sp => sp.GetRequiredService<EcwidHostedService>());
    }
}
