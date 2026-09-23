namespace web.Data.Entities
{
    /// <summary>
    /// A status value available in one ActivityList (e.g. "Ok", "Ikke ok"). Seeded with a default set
    /// when the list is created and editable per list. Exactly one status per list is the default
    /// given to new lines.
    /// </summary>
    public class ActivityListStatus
    {
        public int Id { get; set; }

        public int ActivityListId { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>Color key matching the --clr-* tokens / badge-* classes: muted, info, ok, warn, alarm.</summary>
        public string Color { get; set; } = "muted";

        public bool IsDefault { get; set; }

        public int Order { get; set; }

        public virtual ActivityList ActivityList { get; set; } = null!;
    }
}
