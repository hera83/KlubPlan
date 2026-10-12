namespace web.Repositories.ArrangementRegistrations.Dtos;

/// <summary>One taken slot on a shift. CompanionName null = the registrant themselves.</summary>
public class RegisteredShiftDto
{
    public int ArrangementShiftId { get; set; }
    public string? CompanionName { get; set; }
}
