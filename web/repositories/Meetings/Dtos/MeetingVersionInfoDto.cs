namespace web.Repositories.Meetings.Dtos;

/// <summary>Where a meeting sits in its version series.</summary>
public class MeetingVersionInfoDto
{
    public int VersionNumber { get; set; }
    public int VersionCount { get; set; }
    public bool IsLatestVersion { get; set; } = true;

    /// <summary>The latest version's id, when this isn't it.</summary>
    public int? CurrentVersionId { get; set; }

    public int? PreviousVersionId { get; set; }
    public int? NextVersionId { get; set; }
}
