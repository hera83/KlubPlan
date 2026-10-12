namespace web.Repositories.Forms.Dtos;

/// <summary>Creates (Id 0) or updates a form with its fields. Fields not in the list are deleted (with their answers).</summary>
public class SaveFormDto
{
    /// <summary>0 means create a new form.</summary>
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsAcceptingResponses { get; set; } = true;
    public bool IsAnonymous { get; set; }

    /// <summary>The creator, when creating.</summary>
    public string? UserId { get; set; }

    public List<SaveFormFieldDto> Fields { get; set; } = [];
}
