namespace web.Repositories.ActivityFiles.Dtos
{
    public class MoveActivityFilesResponseDto
    {
        public int MovedCount { get; set; }

        /// <summary>Items that could not be moved, with the reason, e.g. "Budget.xlsx (navnet findes allerede)".</summary>
        public List<string> Skipped { get; set; } = new();
    }
}
