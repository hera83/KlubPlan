namespace web.Data.Entities
{
    /// <summary>
    /// A planned club activity/event (e.g. julebanko, UV-stævne) — metadata plus a target audience
    /// (PersonGroups), a workgroup (plain name/contact records, no system access), a task list, and
    /// an optional link to an existing Form so responses can be managed without duplicating that
    /// logic here. Deleting a linked Form only clears the link (FK is SetNull), mirroring
    /// CommunicationMessage.FormId.
    /// </summary>
    public class Activity
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Location { get; set; }

        /// <summary>Free-text category (e.g. "Fest", "Stævne") — no fixed list.</summary>
        public string? Category { get; set; }

        public DateTime? StartAtUtc { get; set; }

        public DateTime? EndAtUtc { get; set; }

        /// <summary>
        /// Cancelled state can't be derived from the dates, unlike "kommende/i gang/afsluttet"
        /// which the views compute from StartAtUtc/EndAtUtc — so this is the only persisted status.
        /// </summary>
        public bool IsCancelled { get; set; }

        /// <summary>Linked Formular, if any. Kept even if the form is later deleted (FK is SetNull).</summary>
        public int? FormId { get; set; }

        /// <summary>User who created the activity. Nullable so it survives the user being deleted.</summary>
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public virtual Form? Form { get; set; }

        public virtual ICollection<ActivityTargetGroup> TargetGroups { get; set; } = new List<ActivityTargetGroup>();

        public virtual ICollection<ActivityWorkgroupMember> WorkgroupMembers { get; set; } = new List<ActivityWorkgroupMember>();

        public virtual ICollection<ActivityTask> Tasks { get; set; } = new List<ActivityTask>();
    }
}
