namespace web.Repositories.Meetings.Dtos
{
    public class CreateMeetingResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int MeetingId { get; set; }
    }
}
