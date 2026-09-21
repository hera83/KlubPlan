using web.Constants;

namespace web.Data.Entities
{
    /// <summary>
    /// A broadcast message sent to one or more Persons/PersonGroups over Email and/or SMS, with an
    /// optional attached Form link and/or Arrangement (Tilmelding) link. Groups/Persons are stored
    /// (CommunicationMessageGroup / CommunicationMessageRecipientPerson) so a draft can be
    /// re-resolved and (re-)sent later; the actual resolved, deduped send targets are recorded in
    /// CommunicationMessageRecipient.
    /// </summary>
    public class CommunicationMessage
    {
        public int Id { get; set; }

        public string Subject { get; set; } = string.Empty;

        /// <summary>Raw body template, still containing tokens like {{Navn}} before personalization.</summary>
        public string Body { get; set; } = string.Empty;

        public bool ViaEmail { get; set; }

        public bool ViaSms { get; set; }

        public CommunicationMessageStatus Status { get; set; } = CommunicationMessageStatus.Draft;

        /// <summary>Attached form, if any. Kept even if the form is later deleted (FK is SetNull).</summary>
        public int? FormId { get; set; }

        /// <summary>"shared" or "personal" — only meaningful when FormId is set.</summary>
        public string? LinkType { get; set; }

        /// <summary>Attached arrangement (Tilmelding), if any. Kept even if the arrangement is later deleted (FK is SetNull). Its link is always personal — Tilmelding has no anonymous mode.</summary>
        public int? ArrangementId { get; set; }

        /// <summary>Activity this message was sent from, if any. Kept even if the activity is later deleted (FK is SetNull).</summary>
        public int? ActivityId { get; set; }

        /// <summary>Snapshot of the target groups/persons' names at last send, for display without re-resolving.</summary>
        public string RecipientSummary { get; set; } = string.Empty;

        /// <summary>Distinct target Persons reached at last send (not distinct addresses).</summary>
        public int RecipientCount { get; set; }

        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public DateTime? SentAtUtc { get; set; }

        public virtual Form? Form { get; set; }

        public virtual Arrangement? Arrangement { get; set; }

        public virtual Activity? Activity { get; set; }

        public virtual ICollection<CommunicationMessageGroup> Groups { get; set; } = new List<CommunicationMessageGroup>();

        public virtual ICollection<CommunicationMessageRecipientPerson> DirectPersons { get; set; } = new List<CommunicationMessageRecipientPerson>();

        public virtual ICollection<CommunicationMessageRecipient> Recipients { get; set; } = new List<CommunicationMessageRecipient>();
    }
}
