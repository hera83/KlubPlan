using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
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
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

            var pending = await dbContext.CommunicationEmailMessages
                .Where(m => m.Status == CommunicationEmailMessageStatus.Pending)
                .Include(m => m.CommunicationMessage).ThenInclude(cm => cm!.Attachments).ThenInclude(a => a.FileMetadata)
                .OrderBy(m => m.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            if (pending.Count == 0)
            {
                return;
            }

            // Every recipient of the same CommunicationMessage shares the same attachment files —
            // read each physical file once per batch instead of once per recipient.
            var attachmentCache = new Dictionary<int, MailAttachmentDto>();

            async Task<MailAttachmentDto?> LoadAttachmentAsync(FileMetadata fileMetadata)
            {
                if (attachmentCache.TryGetValue(fileMetadata.Id, out var cached))
                {
                    return cached;
                }

                var fullPath = Path.Combine(env.ContentRootPath, fileMetadata.StoredPath);
                if (fileMetadata.IsDeleted || !File.Exists(fullPath))
                {
                    return null;
                }

                var dto = new MailAttachmentDto
                {
                    FileName = fileMetadata.OriginalFileName,
                    ContentType = string.IsNullOrWhiteSpace(fileMetadata.ContentType) ? "application/octet-stream" : fileMetadata.ContentType,
                    Content = await File.ReadAllBytesAsync(fullPath, cancellationToken)
                };
                attachmentCache[fileMetadata.Id] = dto;
                return dto;
            }

            foreach (var message in pending)
            {
                try
                {
                    var attachments = new List<MailAttachmentDto>();
                    foreach (var attachment in message.CommunicationMessage?.Attachments ?? [])
                    {
                        var loaded = await LoadAttachmentAsync(attachment.FileMetadata);
                        if (loaded is not null)
                        {
                            attachments.Add(loaded);
                        }
                    }

                    await mailService.SendMailAsync(
                        new SendMailRequestDto
                        {
                            To = [message.ToAddress],
                            Subject = message.Subject,
                            TextBody = message.Body,
                            HtmlBody = message.HtmlBody,
                            Attachments = attachments
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
