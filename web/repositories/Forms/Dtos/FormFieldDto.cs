using web.Constants;

namespace web.Repositories.Forms.Dtos;

public class FormFieldDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public FormFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }

    /// <summary>The stored options (JSON array), as the builder edits them.</summary>
    public string? OptionsJson { get; set; }

    /// <summary>The options parsed and trimmed (blank ones left out), as the fill-out form shows them.</summary>
    public List<string> Options { get; set; } = [];
}
