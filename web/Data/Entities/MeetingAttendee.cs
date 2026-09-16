namespace web.Data.Entities
{
    /// <summary>
    /// Join entity for the many-to-many relationship between Meeting and the administrators
    /// (ApplicationUser) invited to it. HasAttended is set independently of the invite, so two
    /// admins can be invited but only register the one who actually showed up.
    /// </summary>
    public class MeetingAttendee
    {
        public int MeetingId { get; set; }

        public string ApplicationUserId { get; set; } = string.Empty;

        public bool HasAttended { get; set; }

        public virtual Meeting Meeting { get; set; } = null!;

        public virtual ApplicationUser ApplicationUser { get; set; } = null!;
    }
}
