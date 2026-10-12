using web.Repositories.CommunicationMessages.Dtos;

namespace web.Repositories.CommunicationMessages.Interfaces;

/// <summary>
/// Messages from Kommunikation: drafts, their group/person selections, attachments and — once sent —
/// the recipient rows with their queued SMS/e-mails.
/// </summary>
public interface ICommunicationMessageRepository
{
    /// <summary>Messages newest first — all of them, or only those sent from one activity.</summary>
    Task<List<CommunicationMessageListItemDto>> GetListAsync(int? activityId, CancellationToken cancellationToken = default);

    Task<CommunicationMessageDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Every address the message has been sent to (all sends), with delivery status.</summary>
    Task<List<CommunicationMessageRecipientDto>> GetRecipientsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates (Id = 0) or updates a draft. Only drafts can be updated.</summary>
    Task<SaveCommunicationMessageResultDto> SaveDraftAsync(SaveCommunicationMessageDto dto, CancellationToken cancellationToken = default);

    /// <summary>Links a stored file to the message. False when the message doesn't exist.</summary>
    Task<bool> AddAttachmentAsync(int messageId, int fileMetadataId, CancellationToken cancellationToken = default);

    /// <summary>The stored file (FileMetadata id) behind an attachment, or null.</summary>
    Task<int?> GetAttachmentFileIdAsync(int attachmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds the recipient rows, queues an SMS or e-mail per recipient (picked up by SmsWorker and
    /// CommunicationEmailWorker) and marks the message sent — all in one save. False when not found.
    /// </summary>
    Task<bool> MarkSentAsync(MarkCommunicationMessageSentDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the message with its selections, recipient rows and attachment links. Queued SMS/e-mails
    /// are kept (they are the delivery record). Returns the attachments' stored files, or null when not found.
    /// </summary>
    Task<List<int>?> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
