using web.Constants;

namespace web.Repositories.Arrangements.Dtos;

public class ArrangementFormFieldDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public FormFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }
    public string? OptionsJson { get; set; }

    /// <summary>The options parsed and trimmed (blank ones left out).</summary>
    public List<string> Options { get; set; } = [];
}
