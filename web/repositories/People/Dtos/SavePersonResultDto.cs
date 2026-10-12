namespace web.Repositories.People.Dtos;

/// <summary>Outcome of creating/updating a person. Uid is the UID that was used (or that was already taken).</summary>
public class SavePersonResultDto
{
    public SavePersonStatus Status { get; set; }
    public int PersonId { get; set; }
    public string Uid { get; set; } = string.Empty;
}
