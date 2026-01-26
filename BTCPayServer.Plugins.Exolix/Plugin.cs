using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Abstractions.Services;
using BTCPayServer.Plugins.Exolix.Services;
using Microsoft.AspNetCore.Builder;
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
        services.AddUIExtension("header-nav", "ExolixPluginHeaderNav")
                .AddUIExtension("checkout-payment-method", "CheckoutV2/CheckoutPaymentMethodExtension")
                .AddUIExtension("checkout-payment", "CheckoutV2/CheckoutPaymentExtension")
                .AddSingleton<ExolixPluginDbContextFactory>()
                .AddHostedService<PluginMigrationRunner>()
                .AddSingleton<ExolixService>()
                .AddSingleton<ExolixPluginService>();


    }

}
