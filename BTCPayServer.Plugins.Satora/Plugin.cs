using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Plugins.Satora.Data;
using BTCPayServer.Plugins.Satora.Services;
using uniffi.satora_sdk_ffi;


namespace BTCPayServer.Plugins.Satora;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new() { Identifier = nameof(BTCPayServer), Condition = ">=2.3.7" },
  //      new() { Identifier = "BTCPayServer.Plugins.ArkPayServer", Condition = ">=2.1.0" }

    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("header-nav", "SatoraPluginHeaderNav")
                .AddUIExtension("checkout-payment-method", "CheckoutV2/CheckoutSatoraPaymentMethodExtension")
                .AddUIExtension("checkout-payment", "CheckoutV2/CheckoutSatoraPaymentExtension")
                .AddSingleton<SatoraPluginDbContextFactory>()
                .AddHostedService<PluginMigrationRunner>()
                .AddSingleton(sp =>
                {
                    return new SatoraClient("https://api.lendaswap.com");
                })
                .AddSingleton<SatoraService>()
                .AddSingleton<SatoraPluginService>();


    }

}
