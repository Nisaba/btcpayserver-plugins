using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Plugins.Exolix.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BTCPayServer.Plugins.Exolix;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.3.7" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("header-nav", "ExolixPluginHeaderNav")
                .AddUIExtension("checkout-payment-method", "CheckoutV2/CheckoutPaymentMethodExtension")
                .AddUIExtension("checkout-payment", "CheckoutV2/CheckoutPaymentExtension")
                .AddSingleton<ExolixPluginDbContextFactory>()
                .AddHostedService<PluginMigrationRunner>()
                .AddSingleton<ExolixPluginService>()
                .AddHttpClient<ExolixService>(client =>
                {
                    client.BaseAddress = new Uri(ExolixService.BaseUrl);
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + ExolixService.APIKey);
                });


    }

}
