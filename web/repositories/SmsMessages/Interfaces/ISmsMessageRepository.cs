using web.Repositories.SmsMessages.Dtos;

namespace web.Repositories.SmsMessages.Interfaces;

/// <summary>SMS messages: the outbound queue SmsWorker sends and the inbound messages it fetches.</summary>
public interface ISmsMessageRepository
{
    /// <summary>The SMS log under Indstillinger, newest first. Page size 5-200, otherwise 10.</summary>
    Task<SmsMessagePageDto> SearchAsync(SearchSmsMessagesDto search, CancellationToken cancellationToken = default);

    Task<SmsMessageDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Puts outbound SMS messages in the queue (status Pending) in one save.</summary>
    Task QueueAsync(IReadOnlyCollection<QueueSmsMessageDto> messages, CancellationToken cancellationToken = default);

    /// <summary>Puts a failed outbound message back in the queue.</summary>
    Task<RequeueSmsMessageStatus> RequeueAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    // Used by SmsWorker (via ISmsMessageManager).

    /// <summary>Outbound messages not handed to the gateway yet (no gateway id), oldest first — incl. ones that failed to submit.</summary>
    Task<List<SmsMessageDto>> GetUnsubmittedAsync(CancellationToken cancellationToken = default);

    /// <summary>Records the gateway's answer to a submit (null = no answer, the message stays unsubmitted).</summary>
    Task ApplySubmitResultAsync(int id, SmsGatewayStatusDto? result, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(int id, string failureReason, CancellationToken cancellationToken = default);

    /// <summary>Submitted outbound messages the gateway hasn't reported as sent or failed, created since the given time.</summary>
    Task<List<SmsMessageDto>> GetInFlightAsync(DateTime createdSinceUtc, CancellationToken cancellationToken = default);

    /// <summary>Records a polled gateway status.</summary>
    Task ApplyStatusAsync(int id, SmsGatewayStatusDto status, CancellationToken cancellationToken = default);

    /// <summary>Adds inbound messages not stored yet (by gateway id). Returns how many were added.</summary>
    Task<int> AddInboundAsync(IReadOnlyCollection<InboundSmsMessageDto> messages, CancellationToken cancellationToken = default);
}
