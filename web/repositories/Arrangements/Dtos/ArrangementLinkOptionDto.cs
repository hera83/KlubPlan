namespace web.Repositories.Arrangements.Dtos;

/// <summary>An arrangement that can be linked from a message, with its access rules and registration window.</summary>
public class ArrangementLinkOptionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsRestricted { get; set; }
    public List<int> AllowedPersonIds { get; set; } = [];
    public List<int> AllowedGroupIds { get; set; } = [];
    public DateTime? RegistrationOpensAtUtc { get; set; }
    public DateTime? RegistrationClosesAtUtc { get; set; }
    public bool RegistrationForcedOpen { get; set; }
}
