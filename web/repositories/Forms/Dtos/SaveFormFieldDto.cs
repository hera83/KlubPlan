using web.Constants;

namespace web.Repositories.Forms.Dtos;

public class SaveFormFieldDto
{
    /// <summary>0 for a field that does not exist in the database yet.</summary>
    public int Id { get; set; }

    public string Label { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public FormFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }
    public string? OptionsJson { get; set; }
}
