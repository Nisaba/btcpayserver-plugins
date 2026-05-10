using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Plugins.Satora.Data;
using BTCPayServer.Plugins.Satora.Services;


namespace BTCPayServer.Plugins.Satora;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.3.7" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("header-nav", "SatoraPluginHeaderNav")
                .AddUIExtension("checkout-payment-method", "CheckoutV2/CheckoutPaymentMethodExtension")
                .AddUIExtension("checkout-payment", "CheckoutV2/CheckoutPaymentExtension")
                .AddSingleton<SatoraPluginDbContextFactory>()
                .AddHostedService<PluginMigrationRunner>()
                .AddSingleton<SatoraService>()
                .AddSingleton<SatoraPluginService>();


    }

}
