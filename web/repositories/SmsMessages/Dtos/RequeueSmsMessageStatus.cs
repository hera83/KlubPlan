namespace web.Repositories.SmsMessages.Dtos;

public enum RequeueSmsMessageStatus
{
    Requeued,
    NotFound,

    /// <summary>Only failed outbound messages can be sent again.</summary>
    NotFailed
}
