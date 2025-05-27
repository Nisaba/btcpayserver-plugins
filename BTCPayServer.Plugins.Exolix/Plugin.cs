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
        services.AddUIExtension("header-nav", "ExolixPluginHeaderNav");
        // -- Checkout v2 --
        // Tab (Payment Method)
        services.AddUIExtension("checkout-payment-method", "CheckoutV2/CheckoutPaymentMethodExtension");
        // Widget
        services.AddUIExtension("checkout-payment", "CheckoutV2/CheckoutPaymentExtension");

        // -- Checkout No-Script --
     //   services.AddUIExtension("checkout-noscript-end", "CheckoutNoScript/CheckoutPaymentExtension");
        
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
