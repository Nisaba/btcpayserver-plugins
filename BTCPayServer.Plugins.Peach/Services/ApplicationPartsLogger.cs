using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Peach.Services;

public class ApplicationPartsLogger : IHostedService
{
    private readonly ILogger<ApplicationPartsLogger> _logger;
    private readonly ApplicationPartManager _partManager;

    public ApplicationPartsLogger(ILogger<ApplicationPartsLogger> logger, ApplicationPartManager partManager)
    {
        _logger = logger;
        _partManager = partManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Get the names of all the application parts. This is the short assembly name for AssemblyParts
        IEnumerable<string> applicationParts = _partManager.ApplicationParts.Select(x => x.Name);

        // Create a controller feature, and populate it from the application parts
        ControllerFeature controllerFeature = new ControllerFeature();
        _partManager.PopulateFeature(controllerFeature);

        // Get the names of all of the controllers
        IEnumerable<string> controllers = controllerFeature.Controllers.Select(x => x.Name);

        // Log the application parts and controllers
        _logger.LogInformation(
            "Found the following application parts: '{ApplicationParts}' with the following controllers: '{Controllers}'",
            string.Join(", ", applicationParts), string.Join(", ", controllers));

        return Task.CompletedTask;
    }

    // Required by the interface
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
