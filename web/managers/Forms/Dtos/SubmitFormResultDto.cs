namespace web.Managers.Forms.Dtos;

public class SubmitFormResultDto
{
    public SubmitFormStatus Status { get; set; }

    /// <summary>FormFieldId -> validation error, used to re-highlight the fill-out form.</summary>
    public Dictionary<int, string> FieldErrors { get; set; } = [];
}
