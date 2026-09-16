namespace web.Repositories.Forms.Dtos
{
    public class SaveFormResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int FormId { get; set; }
    }
}
