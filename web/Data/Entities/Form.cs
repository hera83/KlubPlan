namespace web.Data.Entities
{
    /// <summary>
    /// A user-defined form: a title/description plus an ordered list of fields (FormField).
    /// The definition (this + FormField) is separate from collected answers (FormSubmission/FormAnswer)
    /// so editing a form does not have to destroy previously collected responses.
    /// </summary>
    public class Form
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// When false, the form is closed for new responses; existing responses and the
        /// definition are kept. Toggled from the builder without deleting anything.
        /// </summary>
        public bool IsAcceptingResponses { get; set; } = true;

        /// <summary>
        /// Points to the root Form of this version chain. Null means this Form IS the root
        /// (i.e. version 1). All forms sharing the same RootFormId (or, for the root itself, the
        /// same Id) belong to one version series.
        /// </summary>
        public int? RootFormId { get; set; }

        public int VersionNumber { get; set; } = 1;

        /// <summary>
        /// User who created the form. Nullable so the form survives the user being deleted.
        /// </summary>
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ICollection<FormField> Fields { get; set; } = new List<FormField>();

        public virtual ICollection<FormSubmission> Submissions { get; set; } = new List<FormSubmission>();
    }
}
