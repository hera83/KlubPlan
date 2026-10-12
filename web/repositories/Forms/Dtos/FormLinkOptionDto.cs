namespace web.Repositories.Forms.Dtos;

/// <summary>The latest version of a form series that accepts responses — can be linked from a message.</summary>
public class FormLinkOptionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
}
