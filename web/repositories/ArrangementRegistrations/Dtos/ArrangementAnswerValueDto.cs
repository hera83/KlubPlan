namespace web.Repositories.ArrangementRegistrations.Dtos;

public class ArrangementAnswerValueDto
{
    public int ArrangementFormFieldId { get; set; }

    /// <summary>The answer as text (checkbox answers joined with ", "); null when left empty.</summary>
    public string? ValueText { get; set; }
}
