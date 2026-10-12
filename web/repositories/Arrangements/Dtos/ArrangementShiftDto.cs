namespace web.Repositories.Arrangements.Dtos;

public class ArrangementShiftDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string? Location { get; set; }
    public int NeededCount { get; set; }
    public int Order { get; set; }

    /// <summary>In order.</summary>
    public List<ArrangementShiftRequirementDto> Requirements { get; set; } = [];
}
