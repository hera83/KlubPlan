namespace web.Repositories.People.Dtos
{
    public class ImportPersonsToGroupResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int ImportedCount { get; set; }
        public List<ImportPersonErrorDto> Errors { get; set; } = new();
    }

    public class ImportPersonErrorDto
    {
        public string Name { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
