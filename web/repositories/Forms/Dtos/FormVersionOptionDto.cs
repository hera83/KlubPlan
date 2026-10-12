namespace web.Repositories.Forms.Dtos;

public class FormVersionOptionDto
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAcceptingResponses { get; set; }
    public bool IsCurrent { get; set; }
}
