using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Plugins.Shopstr.Data;
using BTCPayServer.Plugins.Shopstr.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Plugins.Shopstr;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new() { Identifier = nameof(BTCPayServer), Condition = ">=2.0.1" },
        new() { Identifier = "BTCPayServer.Plugins.NIP05", Condition = ">=1.1.19" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("header-nav", "ShopstrPluginNav")
                .AddHostedService<PluginMigrationRunner>()
                .AddSingleton<ShopstrDbContextFactory>()
                .AddSingleton<ShopstrService>()
                .AddSingleton<ShopstrPluginService>();

    }

}
