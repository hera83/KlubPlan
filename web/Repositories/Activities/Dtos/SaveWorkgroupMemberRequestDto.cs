namespace web.Repositories.Activities.Dtos
{
    public class SaveWorkgroupMemberRequestDto
    {
        /// <summary>0 = nyt medlem.</summary>
        public int Id { get; set; }

        public int ActivityId { get; set; }

        /// <summary>Set to link this member to a registered administrator instead of a free-text external contact.</summary>
        public string? ApplicationUserId { get; set; }

        /// <summary>Required only when ApplicationUserId is not set.</summary>
        public string? Name { get; set; }

        public string? Role { get; set; }

        public string? Email { get; set; }

        public string? Mobile { get; set; }
    }
}
