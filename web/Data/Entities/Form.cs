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

        /// <summary>
        /// Unguessable id used in the public link (/Formular?Id=..) so hopping between the
        /// sequential, internal Id values can't be used to reach forms one isn't meant to see.
        /// </summary>
        public Guid PublicId { get; set; } = Guid.NewGuid();

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// When false, the form is closed for new responses; existing responses and the
        /// definition are kept. Toggled from the builder without deleting anything.
        /// </summary>
        public bool IsAcceptingResponses { get; set; } = true;

        /// <summary>
        /// When true, submissions to this form are not linked to the submitting user
        /// (FormSubmission.SubmittedByUserId is left null) — used for forms where only the
        /// aggregate answers matter, not who answered.
        /// </summary>
        public bool IsAnonymous { get; set; }

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
