namespace web.Data.Entities
{
    /// <summary>
    /// One completed response to a Form, submitted by a logged-in user.
    /// </summary>
    public class FormSubmission
    {
        public int Id { get; set; }

        public int FormId { get; set; }

        /// <summary>Null when submitted to a Form with IsAnonymous set — the respondent is never recorded.</summary>
        public string? SubmittedByUserId { get; set; }

        /// <summary>
        /// Set instead of SubmittedByUserId when submitted without logging in, via the public
        /// /Formular link with a Person's UId. Also left null on a Form with IsAnonymous set.
        /// </summary>
        public int? SubmittedByPersonId { get; set; }

        public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual Form Form { get; set; } = null!;

        public virtual ICollection<FormAnswer> Answers { get; set; } = new List<FormAnswer>();
    }
}
