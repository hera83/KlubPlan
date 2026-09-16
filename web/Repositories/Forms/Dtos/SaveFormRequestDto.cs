using web.Constants;

namespace web.Repositories.Forms.Dtos
{
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

    public class SaveFormRequestDto
    {
        /// <summary>0 (or omitted) means create a new form.</summary>
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsAcceptingResponses { get; set; } = true;
        public string? UserId { get; set; }
        public List<SaveFormFieldDto> Fields { get; set; } = new();
    }
}
