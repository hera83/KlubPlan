namespace web.Repositories.Activities.Dtos
{
    public class SaveActivityResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int ActivityId { get; set; }
    }
}
