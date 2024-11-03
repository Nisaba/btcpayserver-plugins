using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Plugins.Serilog.Services;
using System.Threading;

namespace BTCPayServer.Plugins.Serilog;

public class PluginRunner : IHostedService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly SerilogService _PluginService;

    public PluginRunner(ISettingsRepository settingsRepository, SerilogService PluginService)
    {
        _settingsRepository = settingsRepository;
        _PluginService = PluginService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {

        await _PluginService.InitSerilogConfig();
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

}
