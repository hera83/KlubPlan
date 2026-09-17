namespace web.Repositories.Meetings.Dtos
{
    public class CreateMeetingRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public DateTime MeetingDateUtc { get; set; }
        public string? Location { get; set; }
        public List<int> GroupIds { get; set; } = new();
        public List<string> AttendeeUserIds { get; set; } = new();
        public string? AgendaNotes { get; set; }
    }
}
