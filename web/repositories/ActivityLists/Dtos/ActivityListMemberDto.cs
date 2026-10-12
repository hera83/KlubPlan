namespace web.Repositories.ActivityLists.Dtos;

/// <summary>A member of the activity's arbejdsgruppe, as a choice for Tilknyttet.</summary>
public class ActivityListMemberDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
    public bool IsLinkedUser { get; set; }
    public bool IsMe { get; set; }
    public int AssignedCount { get; set; }

    /// <summary>Only set for external contacts.</summary>
    public string? Email { get; set; }

    /// <summary>Only set for external contacts.</summary>
    public string? Mobile { get; set; }
    public Guid PublicId { get; set; }
}
