using web.Repositories.CommunicationEmailMessages.Dtos;

namespace web.Repositories.CommunicationEmailMessages.Interfaces;

/// <summary>The outgoing e-mail queue (CommunicationEmailMessages) that CommunicationEmailWorker sends.</summary>
public interface ICommunicationEmailMessageRepository
{
    /// <summary>Queued e-mails not sent yet, oldest first, with their message's attachments.</summary>
    Task<List<PendingCommunicationEmailDto>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>Puts e-mails in the queue (status Pending) in one save.</summary>
    Task QueueAsync(IReadOnlyCollection<QueueCommunicationEmailDto> emails, CancellationToken cancellationToken = default);

    /// <summary>False when the e-mail no longer exists.</summary>
    Task<bool> MarkSentAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>False when the e-mail no longer exists.</summary>
    Task<bool> MarkFailedAsync(int id, string failureReason, CancellationToken cancellationToken = default);
}
