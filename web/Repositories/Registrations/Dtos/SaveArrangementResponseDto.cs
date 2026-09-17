namespace web.Repositories.Registrations.Dtos
{
    public class SaveArrangementResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int ArrangementId { get; set; }
    }
}
