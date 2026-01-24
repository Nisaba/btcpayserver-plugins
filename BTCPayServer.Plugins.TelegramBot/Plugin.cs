using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Plugins.TelegramBot.Data;
using BTCPayServer.Plugins.TelegramBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BTCPayServer.Plugins.TelegramBot;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new() { Identifier = nameof(BTCPayServer), Condition = ">=2.0.1" },
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("header-nav", "TelegramBotPluginNav")
                .AddHostedService<PluginMigrationRunner>()
                .AddSingleton<TelegramBotDbContextFactory>()
                .AddSingleton<TelegramBotPluginService>()
                .AddHostedService<AutoStartService>()
                .AddHostedService<TelegramBotHostedService>();
        ;

    }

}
