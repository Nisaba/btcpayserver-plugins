using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Abstractions.Services;
using BTCPayServer.Plugins.MtPelerin.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Plugins.MtPelerin;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.0.1" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("header-nav", "MtPelerinPluginHeaderNav");
        
        services.AddHostedService<ApplicationPartsLogger>();
        services.AddHostedService<PluginMigrationRunner>();
        services.AddSingleton<MtPelerinPluginService>();
        services.AddSingleton<MtPelerinPluginDbContextFactory>();
        services.AddDbContext<MtPelerinPluginDbContext>((provider, o) =>
        {
            var factory = provider.GetRequiredService<MtPelerinPluginDbContextFactory>();
            factory.ConfigureBuilder(o);
        });

    }

}
