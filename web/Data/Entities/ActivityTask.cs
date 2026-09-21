namespace web.Data.Entities
{
    /// <summary>One to-do item for an Activity, optionally assigned to a workgroup member with a deadline.</summary>
    public class ActivityTask
    {
        public int Id { get; set; }

        public int ActivityId { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime? DeadlineAtUtc { get; set; }

        /// <summary>Kept even if the assigned workgroup member is later removed (FK is SetNull) — the task itself isn't deleted.</summary>
        public int? AssignedToWorkgroupMemberId { get; set; }

        public bool IsCompleted { get; set; }

        /// <summary>Optional note, e.g. added when checking the task off.</summary>
        public string? Note { get; set; }

        public DateTime? CompletedAtUtc { get; set; }

        public string? CompletedByUserId { get; set; }

        public int Order { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual Activity Activity { get; set; } = null!;

        public virtual ActivityWorkgroupMember? AssignedToWorkgroupMember { get; set; }
    }
}
