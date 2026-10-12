namespace web.Repositories.FormSubmissions.Dtos;

/// <summary>A validated response to save. At most one of UserId/PersonId is set (none for anonymous forms).</summary>
public class CreateFormSubmissionDto
{
    public int FormId { get; set; }
    public string? SubmittedByUserId { get; set; }
    public int? SubmittedByPersonId { get; set; }
    public List<FormAnswerValueDto> Answers { get; set; } = [];
}
