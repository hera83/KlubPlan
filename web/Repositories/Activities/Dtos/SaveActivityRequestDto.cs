namespace web.Repositories.Activities.Dtos
{
    public class SaveActivityRequestDto
    {
        /// <summary>0 = ny aktivitet.</summary>
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Location { get; set; }

        public string? Category { get; set; }

        public DateTime? StartAt { get; set; }

        public DateTime? EndAt { get; set; }

        public bool IsCancelled { get; set; }

        public List<int> GroupIds { get; set; } = new();

        public string? UserId { get; set; }
    }
}
