namespace web.Data.Entities
{
    /// <summary>One line in an ActivityList. Column values live in ActivityListCellValue; Status, Note and Tilknyttet are fields here.</summary>
    public class ActivityListItem
    {
        public int Id { get; set; }

        public int ActivityListId { get; set; }

        /// <summary>Position in the list — the Excel row order, new lines are appended at the end.</summary>
        public int Order { get; set; }

        public int? StatusId { get; set; }

        public string? Note { get; set; }

        /// <summary>One workgroup member per line. Cleared (SetNull) if the member is removed from the workgroup.</summary>
        public int? AssignedToWorkgroupMemberId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Last change to any field of the line — shown as "Ændret af … " so it's clear who did what.</summary>
        public DateTime? UpdatedAtUtc { get; set; }

        public string? UpdatedByUserId { get; set; }

        /// <summary>Set instead of UpdatedByUserId when the change came from an external contact via the public /Arbejdsliste link.</summary>
        public int? UpdatedByWorkgroupMemberId { get; set; }

        public virtual ActivityList ActivityList { get; set; } = null!;

        public virtual ActivityListStatus? Status { get; set; }

        public virtual ActivityWorkgroupMember? AssignedToWorkgroupMember { get; set; }

        public virtual ApplicationUser? UpdatedByUser { get; set; }

        public virtual ActivityWorkgroupMember? UpdatedByWorkgroupMember { get; set; }

        public virtual ICollection<ActivityListCellValue> Values { get; set; } = new List<ActivityListCellValue>();

        public virtual ICollection<ActivityListLabel> Labels { get; set; } = new List<ActivityListLabel>();
    }
}
