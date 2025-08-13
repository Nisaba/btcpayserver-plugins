using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Plugins.LnOnchainSwaps.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.LnOnchainSwaps;

public class PluginMigrationRunner : IHostedService
{

    public async Task StartAsync(CancellationToken cancellationToken)
    {

    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private class PluginDataMigrationHistory
    {
        public bool UpdatedSomething { get; set; }
    }
}

