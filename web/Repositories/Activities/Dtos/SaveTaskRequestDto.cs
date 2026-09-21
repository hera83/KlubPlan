namespace web.Repositories.Activities.Dtos
{
    public class SaveTaskRequestDto
    {
        /// <summary>0 = ny opgave.</summary>
        public int Id { get; set; }

        public int ActivityId { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime? DeadlineAt { get; set; }

        public int? AssignedToWorkgroupMemberId { get; set; }

        public string? Note { get; set; }
    }
}
