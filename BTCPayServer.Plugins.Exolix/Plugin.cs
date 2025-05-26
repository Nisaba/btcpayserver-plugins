using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Abstractions.Services;
using BTCPayServer.Plugins.Exolix.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Plugins.Exolix;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.0.1" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("header-nav", "ExolixPluginHeaderNav");
        services.AddHostedService<ApplicationPartsLogger>();
        services.AddHostedService<PluginMigrationRunner>();
        services.AddSingleton<ExolixPluginService>();
        services.AddSingleton<ExolixService>();
        services.AddSingleton<ExolixPluginDbContextFactory>();
        services.AddDbContext<ExolixPluginDbContext>((provider, o) =>
        {
            var factory = provider.GetRequiredService<ExolixPluginDbContextFactory>();
            factory.ConfigureBuilder(o);
        });
    }
}
