namespace web.Repositories.LabelCollections.Dtos
{
    public class SaveLabelCollectionResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>Success text for the toast.</summary>
        public string? Message { get; set; }

        public int? Id { get; set; }

        public static SaveLabelCollectionResponseDto Ok(int id, string message) => new() { Success = true, Id = id, Message = message };
        public static SaveLabelCollectionResponseDto Fail(string error) => new() { Success = false, ErrorMessage = error };
    }
}
