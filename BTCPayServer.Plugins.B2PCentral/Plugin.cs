using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Abstractions.Services;
using BTCPayServer.Plugins.B2PCentral.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Plugins.B2PCentral;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=1.12.0" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddSingleton<IUIExtension>(new UIExtension("B2PCentralPluginHeaderNav", "header-nav"));
        services.AddHostedService<ApplicationPartsLogger>();
        services.AddHostedService<PluginMigrationRunner>();
        services.AddSingleton<B2PCentralPluginService>();
        services.AddSingleton<B2PCentralPluginDbContextFactory>();
        services.AddDbContext<B2PCentralPluginDbContext>((provider, o) =>
        {
            var factory = provider.GetRequiredService<B2PCentralPluginDbContextFactory>();
            factory.ConfigureBuilder(o);
        });
    }
}
