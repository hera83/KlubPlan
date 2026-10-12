namespace web.Repositories.ActivityLists.Dtos;

public class ActivityListLinkMemberDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public Guid PublicId { get; set; }

    /// <summary>Lines on the list assigned to the member.</summary>
    public int AssignedCount { get; set; }
}
