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

        /// <summary>
        /// HTML rendering of Body — same content, with links as clickable &lt;a href&gt; tags and
        /// line breaks as &lt;br&gt;. Sent as the HTML alternative alongside Body (plain text) so
        /// email clients that support HTML show real hyperlinks. Null falls back to plain text
        /// only. Never used for SMS — SmsMessage.Body stays plain text regardless.
        /// </summary>
        public string? HtmlBody { get; set; }

        public CommunicationEmailMessageStatus Status { get; set; } = CommunicationEmailMessageStatus.Pending;

        public string? FailureReason { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public DateTime? SentAtUtc { get; set; }

        public DateTime? FailedAtUtc { get; set; }
    }
}
