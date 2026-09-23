namespace web.Repositories.ActivityLists.Dtos
{
    /// <summary>Result of an inline edit — includes fresh status counts so the page's progress bar can update without a reload.</summary>
    public class ActivityListFieldUpdateResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<int, int> StatusCounts { get; set; } = new();
        public int UnassignedCount { get; set; }

        /// <summary>"Ændret af X · 22. sep. 2026 14:30" for the line's history hint.</summary>
        public string? UpdatedText { get; set; }
    }
}
