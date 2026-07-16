using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Plugins.B2PCentral.Data;
using BTCPayServer.Plugins.B2PCentral.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BTCPayServer.Plugins.B2PCentral;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.3.7" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddHttpClient<B2PCentralService>(client =>
        {
            client.BaseAddress = new Uri(B2PCentralService.BaseApiUrl);
        });

        services.AddUIExtension("header-nav", "B2PCentralPluginHeaderNav")
                .AddUIExtension("checkout-payment-method", "CheckoutV2/B2PCheckoutPaymentMethodExtension")
                .AddUIExtension("checkout-payment", "CheckoutV2/B2PCheckoutPaymentExtension")
                .AddHostedService<PluginMigrationRunner>()
                .AddSingleton<B2PCentralPluginService>()
                .AddSingleton<B2PCentralPluginDbContextFactory>()
                .AddSingleton<B2PAutoSwapHostedService>()
                    .AddHostedService(sp => sp.GetRequiredService<B2PAutoSwapHostedService>());

    }
}
