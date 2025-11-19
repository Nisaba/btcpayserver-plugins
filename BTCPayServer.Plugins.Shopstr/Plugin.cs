using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Abstractions.Services;
using BTCPayServer.Plugins.Shopstr.Data;
using BTCPayServer.Plugins.Shopstr.Services;
using Microsoft.AspNetCore.Builder;
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
        services.AddUIExtension("header-nav", "ShopstrPluginNav");

        services.AddHostedService<ApplicationPartsLogger>();
        services.AddHostedService<PluginMigrationRunner>();
        services.AddSingleton<ShopstrDbContextFactory>();
        services.AddDbContext<ShopstrDbContext>((provider, o) =>
        {
            var factory = provider.GetRequiredService<ShopstrDbContextFactory>();
            factory.ConfigureBuilder(o);
        });
        services.AddSingleton<ShopstrPluginService>();
        services.AddSingleton<ShopstrService>();

    }

}
