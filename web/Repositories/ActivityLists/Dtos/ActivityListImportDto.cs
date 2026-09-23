namespace web.Repositories.ActivityLists.Dtos
{
    /// <summary>Parsed Excel sheet: header names and the data rows (same length as Headers).</summary>
    public class ActivityListImportDto
    {
        public List<string> Headers { get; set; } = new();
        public List<string?[]> Rows { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public bool Success => ErrorMessage is null;
    }
}
