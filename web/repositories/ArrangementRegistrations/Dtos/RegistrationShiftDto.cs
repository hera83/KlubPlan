namespace web.Repositories.ArrangementRegistrations.Dtos;

/// <summary>A taken slot on a shift, with the shift's title and times (UTC).</summary>
public class RegistrationShiftDto
{
    public int ArrangementShiftId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    /// <summary>Null = the registrant themselves.</summary>
    public string? CompanionName { get; set; }
}
