namespace web.Repositories.Forms.Dtos
{
    public class SubmitFormResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>FormFieldId -> validation error, used to re-highlight the fill-out form.</summary>
        public Dictionary<int, string> FieldErrors { get; set; } = new();
    }
}
