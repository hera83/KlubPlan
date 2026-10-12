namespace web.Repositories.Forms.Dtos;

/// <summary>What is needed to build a public /Formular link.</summary>
public class FormLinkDto
{
    public int Id { get; set; }
    public Guid PublicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
}
