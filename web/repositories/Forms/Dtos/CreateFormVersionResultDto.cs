namespace web.Repositories.Forms.Dtos;

public class CreateFormVersionResultDto
{
    public CreateFormVersionStatus Status { get; set; }

    /// <summary>The new version's id (when Created).</summary>
    public int FormId { get; set; }
}
