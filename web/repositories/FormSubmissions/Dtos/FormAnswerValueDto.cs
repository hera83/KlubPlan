namespace web.Repositories.FormSubmissions.Dtos;

public class FormAnswerValueDto
{
    public int FormFieldId { get; set; }

    /// <summary>The answer as text (checkbox answers joined with ", "); null when left empty.</summary>
    public string? ValueText { get; set; }
}
