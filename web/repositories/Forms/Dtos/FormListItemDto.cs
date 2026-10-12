namespace web.Repositories.Forms.Dtos;

/// <summary>The latest version of a form series, as the forms table shows it.</summary>
public class FormListItemDto
{
    public int Id { get; set; }
    public Guid PublicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsAcceptingResponses { get; set; }
    public bool IsAnonymous { get; set; }
    public int FieldCount { get; set; }
    public int ResponseCount { get; set; }
    public bool HasCurrentUserSubmitted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int VersionNumber { get; set; }
    public int VersionCount { get; set; }

    /// <summary>All versions in the series, newest first — only filled when there is more than one.</summary>
    public List<FormVersionOptionDto> Versions { get; set; } = [];
}
