using web.Managers.SmsMessages.Dtos;

namespace web.Managers.SmsMessages.Interfaces;

/// <summary>Keeps the SmsMessages table in sync with the SMS gateway (ISmsService).</summary>
public interface ISmsMessageManager
{
    /// <summary>Hands every unsubmitted outbound message to the gateway; failures are marked Failed (and retried next run).</summary>
    Task SendOutboundAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the status of submitted messages from the last 7 days that aren't final yet — a submit
    /// only tells that the gateway accepted the message, not when it was actually sent.
    /// </summary>
    Task PollOutboundStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>Stores new inbound messages from the gateway.</summary>
    Task FetchInboundAsync(CancellationToken cancellationToken = default);

    /// <summary>Deletes a Queued or Failed message — a queued one is cancelled in the gateway first.</summary>
    Task<DeleteSmsMessageStatus> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
