using BTCPayServer.Plugins.Serilog.Services;
using Microsoft.Extensions.Hosting;

namespace BTCPayServer.Plugins.Serilog;

public class PluginRunner(SerilogService pluginService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await pluginService.InitSerilogConfig(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
