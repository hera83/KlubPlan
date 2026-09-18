using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Services.Mail.Dtos;
using web.Services.Mail.Interfaces;

namespace web.BgSerives
{
    /// <summary>
    /// Sends queued outbound emails from the Communication feature via IMailService, mirroring
    /// SmsWorker's outbound-send loop. SMTP is fire-and-forget (no gateway status to poll), so
    /// unlike SmsWorker there is no separate status-polling pass.
    /// </summary>
    public class CommunicationEmailWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CommunicationEmailWorker> _logger;
        private readonly TimeSpan _interval;

        public CommunicationEmailWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<CommunicationEmailWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var intervalSeconds = configuration.GetValue<int?>("Communication:EmailWorkerIntervalSeconds") ?? 30;
            _interval = TimeSpan.FromSeconds(intervalSeconds > 0 ? intervalSeconds : 30);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            while (true)
            {
                try
                {
                    await SendPendingEmailsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CommunicationEmailWorker iteration failed");
                }

                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task SendPendingEmailsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();

            var pending = await dbContext.CommunicationEmailMessages
                .Where(m => m.Status == CommunicationEmailMessageStatus.Pending)
                .OrderBy(m => m.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            if (pending.Count == 0)
            {
                return;
            }

            foreach (var message in pending)
            {
                try
                {
                    await mailService.SendMailAsync(
                        new SendMailRequestDto
                        {
                            To = [message.ToAddress],
                            Subject = message.Subject,
                            TextBody = message.Body
                        },
                        cancellationToken);

                    message.Status = CommunicationEmailMessageStatus.Sent;
                    message.SentAtUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send CommunicationEmailMessage {Id} to {ToAddress}", message.Id, message.ToAddress);
                    message.Status = CommunicationEmailMessageStatus.Failed;
                    message.FailureReason = ex.Message;
                    message.FailedAtUtc = DateTime.UtcNow;
                }

                message.UpdatedAtUtc = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
