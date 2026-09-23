namespace web.Repositories.ActivityFiles.Dtos
{
    /// <summary>Shared success/error result for folder/file mutations (create, rename, restore, ...).</summary>
    public class ActivityFileActionResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>Id of the created/affected folder or file, when relevant.</summary>
        public int? Id { get; set; }

        public static ActivityFileActionResultDto Ok(int? id = null) => new() { Success = true, Id = id };
        public static ActivityFileActionResultDto Fail(string message) => new() { Success = false, ErrorMessage = message };
    }
}
