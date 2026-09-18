using web.Constants;

namespace web.Data.Entities
{
    /// <summary>
    /// A single outbound email queued by the Communication feature. Sent by CommunicationEmailWorker
    /// via IMailService, mirroring how SmsMessage/SmsWorker queue outbound SMS.
    /// </summary>
    public class CommunicationEmailMessage
    {
        public int Id { get; set; }

        public string ToAddress { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public CommunicationEmailMessageStatus Status { get; set; } = CommunicationEmailMessageStatus.Pending;

        public string? FailureReason { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public DateTime? SentAtUtc { get; set; }

        public DateTime? FailedAtUtc { get; set; }
    }
}
