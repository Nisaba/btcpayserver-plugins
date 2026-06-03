using Microsoft.EntityFrameworkCore;
using BTCPayServer.Plugins.Satora.Data;

namespace BTCPayServer.Plugins.Satora.Services;

public class SatoraSwapWatcher(
        SatoraPluginDbContextFactory dbContextFactory,
        SatoraPluginService pluginService,
        ILogger<SatoraSwapWatcher> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    private static readonly string[] TerminalStatuses = 
    {
        "ClientRedeemed", "ServerRedeemed", "ClientRefunded",
        "ClientFundedServerRefunded", "ClientRefundedServerFunded",
        "ClientRefundedServerRefunded", "Expired", "ClientInvalidFunded",
        "ClientFundedTooLate", "ServerWontFund"
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessSwapsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing Satora swaps.");
            }
        }
    }

    private async Task ProcessSwapsAsync(CancellationToken cancellationToken)
    {
        using var context = dbContextFactory.CreateContext();
        
        var activeSwaps = await context.SatoraTransactions
            .Where(tx => tx.Status == null || !TerminalStatuses.Contains(tx.Status))
            .ToListAsync(cancellationToken);

        foreach (var tx in activeSwaps)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var initialStatus = tx.Status;

            try
            {
                var (action, newStatus) = await pluginService.ContinueSwapAsync(tx.TxID);

                if (initialStatus != newStatus)
                {
                    logger.LogInformation("Swap {TxID} transitioned from {OldStatus} to {NewStatus}. Action taken: {Action}", 
                        tx.TxID, initialStatus ?? "null", newStatus, action);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to process swap {TxID}. It will be retried on the next run.", tx.TxID);
            }
        }
    }
}