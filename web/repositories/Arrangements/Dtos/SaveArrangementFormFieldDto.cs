using web.Constants;

namespace web.Repositories.Arrangements.Dtos;

public class SaveArrangementFormFieldDto
{
    /// <summary>0 for a new field.</summary>
    public int Id { get; set; }

    public string Label { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public FormFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }
    public string? OptionsJson { get; set; }
}
