namespace web.Repositories.Forms.Dtos;

/// <summary>Where a form sits in its version series (for the version navigation in builder and responses).</summary>
public class FormVersionInfoDto
{
    public int VersionNumber { get; set; }
    public int VersionCount { get; set; }
    public bool IsLatestVersion { get; set; }

    /// <summary>The latest version's id, when this isn't it.</summary>
    public int? CurrentVersionId { get; set; }

    public int? PreviousVersionId { get; set; }
    public int? NextVersionId { get; set; }
}
