namespace web.Repositories.Communication.Dtos
{
    public class SaveMessageResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int MessageId { get; set; }
    }
}
