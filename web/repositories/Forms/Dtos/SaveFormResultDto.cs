namespace web.Repositories.Forms.Dtos;

public class SaveFormResultDto
{
    public SaveFormStatus Status { get; set; }
    public int FormId { get; set; }

    /// <summary>Set when Status is Invalid.</summary>
    public string? ErrorMessage { get; set; }
}
