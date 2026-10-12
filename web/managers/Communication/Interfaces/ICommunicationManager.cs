using web.Managers.Communication.Dtos;
using web.Repositories.Files.Dtos;

namespace web.Managers.Communication.Interfaces;

/// <summary>Kommunikation: composing, sending and following up on messages to persons/groups via e-mail and SMS.</summary>
public interface ICommunicationManager
{
    /// <summary>Groups, persons, forms and arrangements for the compose modal.</summary>
    Task<ComposeOptionsDto> GetComposeOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Messages newest first — all, or only those sent from one activity.</summary>
    Task<CommunicationMessageListDto> GetMessagesAsync(int? activityId, CancellationToken cancellationToken = default);

    Task<CommunicationMessageDetailsDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The message's full target audience — also people without contact info — filtered and paged. Null when not found.</summary>
    Task<CommunicationRecipientPageDto?> GetRecipientsAsync(int id, SearchCommunicationRecipientsDto search, CancellationToken cancellationToken = default);

    /// <summary>The target audience for the "Send igen" picker, flagging who received the message before. Empty when not found.</summary>
    Task<List<CommunicationResendTargetDto>> GetResendTargetsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Validates and saves a draft (with e-mail attachments), then sends it when <see cref="SaveCommunicationDto.Send"/> is set.</summary>
    Task<SendCommunicationResultDto> SaveAsync(SaveCommunicationDto dto, string baseUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an existing message — a saved draft or "Send igen". With <paramref name="recipientPersonIds"/>
    /// only those persons get it, limited to the message's own target audience.
    /// </summary>
    Task<SendCommunicationResultDto> SendExistingAsync(int id, string baseUrl, IReadOnlyCollection<int>? recipientPersonIds, CancellationToken cancellationToken = default);

    /// <summary>Deletes the message and its attachment files. False when not found.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The attachment's stored file for download, or null when it or the file is gone.</summary>
    Task<StoredFileDto?> GetAttachmentFileAsync(int attachmentId, CancellationToken cancellationToken = default);
}
