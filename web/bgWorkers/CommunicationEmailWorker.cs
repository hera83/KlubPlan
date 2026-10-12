using web.Managers.CommunicationEmails.Interfaces;

namespace web.BgWorkers;

/// <summary>
/// Sends queued outbound e-mails from Kommunikation (ICommunicationEmailManager.SendPendingAsync),
/// mirroring SmsWorker's outbound-send loop. SMTP is fire-and-forget (no gateway status to poll),
/// so unlike SmsWorker there is no separate status-polling pass.
/// </summary>
public class CommunicationEmailWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<CommunicationEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = configuration.GetValue("Communication:WorkerIntervalSeconds", 30);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds > 0 ? intervalSeconds : 30));
        try
        {
            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var communicationEmailManager = scope.ServiceProvider.GetRequiredService<ICommunicationEmailManager>();
                    await communicationEmailManager.SendPendingAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Forventet ved nedlukning.
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Worker} failed", nameof(CommunicationEmailWorker));
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // WaitForNextTickAsync kaster, når stoppingToken annulleres.
        }
    }
}
