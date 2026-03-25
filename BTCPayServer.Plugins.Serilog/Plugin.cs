using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Plugins.Serilog.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Plugins.Serilog;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    [
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.3.7" }
    ];

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("server-nav", "SerilogPluginServerNav")
                .AddHostedService<PluginRunner>()
                .AddSingleton<SerilogService>();
    }
}
