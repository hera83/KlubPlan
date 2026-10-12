namespace web.Managers.Arrangements.Dtos;

/// <summary>An answer as posted from the sign-up form.</summary>
public class ArrangementAnswerInputDto
{
    public int ArrangementFormFieldId { get; set; }
    public string? Value { get; set; }

    /// <summary>Checkbox fields post several values.</summary>
    public List<string> Values { get; set; } = [];
}
