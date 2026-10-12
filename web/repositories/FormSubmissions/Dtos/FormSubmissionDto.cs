namespace web.Repositories.FormSubmissions.Dtos;

/// <summary>One response row: who answered (user name, person name or "Anonym") and the answers per field.</summary>
public class FormSubmissionDto
{
    public int Id { get; set; }
    public string SubmittedByDisplayName { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
    public Dictionary<int, string> Answers { get; set; } = [];
}
