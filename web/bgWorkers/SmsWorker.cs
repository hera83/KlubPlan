using web.Managers.SmsMessages.Interfaces;

namespace web.BgWorkers;

/// <summary>
/// Sends queued outbound SMS messages, refreshes their gateway status and pulls new inbound SMS
/// messages via <see cref="ISmsMessageManager"/>, keeping the SmsMessages table in sync with the SMS gateway.
/// </summary>
public class SmsWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SmsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = configuration.GetValue("Sms:WorkerIntervalSeconds", 30);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds > 0 ? intervalSeconds : 30));
        try
        {
            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var smsMessageManager = scope.ServiceProvider.GetRequiredService<ISmsMessageManager>();
                    await smsMessageManager.SendOutboundAsync(stoppingToken);
                    await smsMessageManager.PollOutboundStatusesAsync(stoppingToken);
                    await smsMessageManager.FetchInboundAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Forventet ved nedlukning.
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Worker} failed", nameof(SmsWorker));
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // WaitForNextTickAsync kaster, når stoppingToken annulleres.
        }
    }
}
