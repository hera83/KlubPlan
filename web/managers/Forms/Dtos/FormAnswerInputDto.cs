namespace web.Managers.Forms.Dtos;

/// <summary>An answer as posted from a fill-out form.</summary>
public class FormAnswerInputDto
{
    public int FormFieldId { get; set; }
    public string? Value { get; set; }

    /// <summary>Checkbox fields post several values.</summary>
    public List<string> Values { get; set; } = [];
}
