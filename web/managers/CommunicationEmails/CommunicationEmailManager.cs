using web.Managers.CommunicationEmails.Interfaces;
using web.Repositories.CommunicationEmailMessages.Interfaces;
using web.Repositories.Files.Interfaces;
using web.Services.Mail.Dtos;
using web.Services.Mail.Interfaces;

namespace web.Managers.CommunicationEmails;

public class CommunicationEmailManager(
    ICommunicationEmailMessageRepository communicationEmailMessageRepository,
    IFileRepository fileRepository,
    IMailService mailService,
    ILogger<CommunicationEmailManager> logger) : ICommunicationEmailManager
{
    public async Task SendPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await communicationEmailMessageRepository.GetPendingAsync(cancellationToken);
        if (pending.Count == 0)
        {
            return;
        }

        // Every recipient of the same message shares the same attachment files — read each
        // file once per batch instead of once per recipient. Null = missing on disk.
        var attachmentCache = new Dictionary<int, MailAttachmentDto?>();

        foreach (var message in pending)
        {
            try
            {
                var attachments = new List<MailAttachmentDto>();
                foreach (var fileId in message.AttachmentFileIds)
                {
                    if (!attachmentCache.TryGetValue(fileId, out var attachment))
                    {
                        attachment = await LoadAttachmentAsync(fileId, cancellationToken);
                        attachmentCache[fileId] = attachment;
                    }

                    if (attachment is not null)
                    {
                        attachments.Add(attachment);
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

                await communicationEmailMessageRepository.MarkSentAsync(message.Id, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Failed to send CommunicationEmailMessage {Id}", message.Id);
                await communicationEmailMessageRepository.MarkFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }

    private async Task<MailAttachmentDto?> LoadAttachmentAsync(int fileId, CancellationToken cancellationToken)
    {
        var file = await fileRepository.GetByIdAsync(fileId, cancellationToken);
        if (file is null)
        {
            return null;
        }

        return new MailAttachmentDto
        {
            FileName = file.OriginalFileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            Content = await File.ReadAllBytesAsync(file.FullPath, cancellationToken)
        };
    }
}
