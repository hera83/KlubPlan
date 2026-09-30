namespace web.Repositories.LabelCollections.Dtos
{
    /// <summary>Success/error result for label collection changes.</summary>
    public class LabelCollectionActionResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>Success text for the toast.</summary>
        public string? Message { get; set; }

        /// <summary>Id of the created/affected collection.</summary>
        public int? Id { get; set; }

        /// <summary>The uploaded image (UploadMediaAsync).</summary>
        public LabelMediaDto? Media { get; set; }

        public static LabelCollectionActionResultDto Ok(string message, int? id = null) => new() { Success = true, Message = message, Id = id };
        public static LabelCollectionActionResultDto Fail(string error) => new() { Success = false, ErrorMessage = error };
    }
}
