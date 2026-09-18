namespace web.Data.Entities
{
    /// <summary>
    /// One resolved, deduped send target for a CommunicationMessage: a single (Channel, Address)
    /// actually queued for delivery, attributed to the target Person it was resolved for (used for
    /// {{Navn}} personalization and personal form links even when the address belongs to a guardian).
    /// This is the audit trail behind the "Vis besked" detail view.
    /// </summary>
    public class CommunicationMessageRecipient
    {
        public int Id { get; set; }

        public int CommunicationMessageId { get; set; }

        /// <summary>The club member this address was resolved for (own contact or one of their guardians').</summary>
        public int PersonId { get; set; }

        /// <summary>Snapshot of the target Person's name at send time.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>CommunicationChannel.Email or CommunicationChannel.Sms.</summary>
        public string Channel { get; set; } = string.Empty;

        /// <summary>The email address or phone number actually used.</summary>
        public string Address { get; set; } = string.Empty;

        public int? SmsMessageId { get; set; }

        public int? CommunicationEmailMessageId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual CommunicationMessage CommunicationMessage { get; set; } = null!;

        public virtual Person Person { get; set; } = null!;

        public virtual SmsMessage? SmsMessage { get; set; }

        public virtual CommunicationEmailMessage? CommunicationEmailMessage { get; set; }
    }
}
