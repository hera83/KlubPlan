namespace web.Repositories.Forms.Dtos;

/// <summary>One form (one version of a form series) with its fields in order.</summary>
public class FormDto
{
    public int Id { get; set; }
    public Guid PublicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsAcceptingResponses { get; set; }
    public bool IsAnonymous { get; set; }
    public int VersionNumber { get; set; }

    /// <summary>The first version's id; null when this is the first version.</summary>
    public int? RootFormId { get; set; }

    public List<FormFieldDto> Fields { get; set; } = [];
}
