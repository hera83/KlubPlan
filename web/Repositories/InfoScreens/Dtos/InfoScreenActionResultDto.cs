namespace web.Repositories.InfoScreens.Dtos
{
    /// <summary>Success/error result for info screen changes.</summary>
    public class InfoScreenActionResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>Success text for the toast.</summary>
        public string? Message { get; set; }

        /// <summary>Id of the created/affected screen.</summary>
        public int? Id { get; set; }

        /// <summary>The uploaded file (UploadMediaAsync).</summary>
        public InfoScreenMediaDto? Media { get; set; }

        public static InfoScreenActionResultDto Ok(string message, int? id = null) => new() { Success = true, Message = message, Id = id };
        public static InfoScreenActionResultDto Fail(string error) => new() { Success = false, ErrorMessage = error };
    }
}
