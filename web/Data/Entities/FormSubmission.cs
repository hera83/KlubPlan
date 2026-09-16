namespace web.Data.Entities
{
    /// <summary>
    /// One completed response to a Form, submitted by a logged-in user.
    /// </summary>
    public class FormSubmission
    {
        public int Id { get; set; }

        public int FormId { get; set; }

        public string SubmittedByUserId { get; set; } = string.Empty;

        public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual Form Form { get; set; } = null!;

        public virtual ICollection<FormAnswer> Answers { get; set; } = new List<FormAnswer>();
    }
}
