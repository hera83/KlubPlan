namespace web.Repositories.ArrangementRegistrations.Dtos;

/// <summary>One registration row: the person, when, the answers per field and the taken shift slots.</summary>
public class ArrangementRegistrationDto
{
    public int Id { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public DateTime RegisteredAtUtc { get; set; }
    public Dictionary<int, string> Answers { get; set; } = [];
    public List<RegistrationShiftDto> Shifts { get; set; } = [];
}
