namespace web.Repositories.Activities.Dtos;

public class SaveActivityDto
{
    /// <summary>0 = new activity.</summary>
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Category { get; set; }
    public DateTime? StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public bool IsCancelled { get; set; }
    public List<int> GroupIds { get; set; } = [];

    /// <summary>Only used when creating.</summary>
    public string? CreatedByUserId { get; set; }
}
