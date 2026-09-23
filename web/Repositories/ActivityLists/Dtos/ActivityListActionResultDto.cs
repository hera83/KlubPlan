namespace web.Repositories.ActivityLists.Dtos
{
    /// <summary>Shared success/error result for list mutations.</summary>
    public class ActivityListActionResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>Success text for the toast, when the action has a more specific one than the controller's default.</summary>
        public string? Message { get; set; }

        /// <summary>Id of the created/affected list, line or column.</summary>
        public int? Id { get; set; }

        public static ActivityListActionResultDto Ok(int? id = null, string? message = null) => new() { Success = true, Id = id, Message = message };
        public static ActivityListActionResultDto Fail(string error) => new() { Success = false, ErrorMessage = error };
    }
}
