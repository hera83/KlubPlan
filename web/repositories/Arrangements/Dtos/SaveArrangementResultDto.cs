namespace web.Repositories.Arrangements.Dtos;

public class SaveArrangementResultDto
{
    public SaveArrangementStatus Status { get; set; }
    public int ArrangementId { get; set; }

    /// <summary>Set when Status is Invalid.</summary>
    public string? ErrorMessage { get; set; }
}
