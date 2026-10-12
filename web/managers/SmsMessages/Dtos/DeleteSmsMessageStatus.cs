namespace web.Managers.SmsMessages.Dtos;

public enum DeleteSmsMessageStatus
{
    Deleted,
    NotFound,

    /// <summary>Only Queued or Failed messages can be deleted.</summary>
    NotDeletable,

    /// <summary>The gateway couldn't cancel a queued message (it may already be sent).</summary>
    GatewayFailed
}
