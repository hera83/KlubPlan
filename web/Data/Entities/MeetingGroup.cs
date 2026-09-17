namespace web.Data.Entities
{
    /// <summary>
    /// Join entity for the many-to-many relationship between Meeting and PersonGroup, so a
    /// meeting can concern several groups at once (mirrors PersonGroupMembership).
    /// </summary>
    public class MeetingGroup
    {
        public int MeetingId { get; set; }

        public int PersonGroupId { get; set; }

        public virtual Meeting Meeting { get; set; } = null!;

        public virtual PersonGroup PersonGroup { get; set; } = null!;
    }
}
