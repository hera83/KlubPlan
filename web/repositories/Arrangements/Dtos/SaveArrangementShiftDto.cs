namespace web.Repositories.Arrangements.Dtos;

public class SaveArrangementShiftDto
{
    /// <summary>0 for a new shift.</summary>
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string? Location { get; set; }
    public int NeededCount { get; set; }
    public int Order { get; set; }

    /// <summary>Requirements with a blank text are skipped.</summary>
    public List<SaveArrangementShiftRequirementDto> Requirements { get; set; } = [];
}
