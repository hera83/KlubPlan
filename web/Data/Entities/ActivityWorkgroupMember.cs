namespace web.Data.Entities
{
    /// <summary>
    /// A member of an Activity's arbejdsgruppe — either a registered administrator (ApplicationUserId
    /// set, name/contact info comes from that account, mirrors MeetingDecision.ResponsibleUserId so a
    /// future "my tasks" dashboard can query ActivityTask by ApplicationUserId the same way a meeting's
    /// decisions are queried by responsible user) or a plain external contact (no login/system access —
    /// Name/Email/Mobile store the free-text info directly). Exactly one of the two applies per member.
    /// </summary>
    public class ActivityWorkgroupMember
    {
        public int Id { get; set; }

        public int ActivityId { get; set; }

        /// <summary>Linked administrator, if any. Kept even if the user is later deleted (FK is SetNull) — the member row survives as an unlinked entry.</summary>
        public string? ApplicationUserId { get; set; }

        /// <summary>External contact's name. Only used (and required) when ApplicationUserId is null — a linked administrator's name comes from ApplicationUser.DisplayName.</summary>
        public string? Name { get; set; }

        /// <summary>Free-text role/title within the workgroup, e.g. "Tovholder", "Bager" — applies to both linked and external members.</summary>
        public string? Role { get; set; }

        /// <summary>Only used when ApplicationUserId is null — a linked administrator's contact info comes from their account.</summary>
        public string? Email { get; set; }

        /// <summary>Only used when ApplicationUserId is null — a linked administrator's contact info comes from their account.</summary>
        public string? Mobile { get; set; }

        public int Order { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual Activity Activity { get; set; } = null!;

        public virtual ApplicationUser? ApplicationUser { get; set; }

        public virtual ICollection<ActivityTask> AssignedTasks { get; set; } = new List<ActivityTask>();
    }
}
