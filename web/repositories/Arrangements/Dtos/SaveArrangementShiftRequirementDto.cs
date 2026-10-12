namespace web.Repositories.Arrangements.Dtos;

public class SaveArrangementShiftRequirementDto
{
    /// <summary>0 for a new requirement.</summary>
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
}
