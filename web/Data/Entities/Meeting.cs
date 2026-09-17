using web.Constants;

namespace web.Data.Entities
{
    /// <summary>
    /// A meeting held by the club's administrators. Notes are split into an agenda (written
    /// before the meeting) and minutes (written during/after), since mixing the two is the
    /// classic failure mode of meeting tools. Optionally tagged with the PersonGroups the
    /// meeting concerns (e.g. specific teams), independent of who attended.
    /// </summary>
    public class Meeting
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime MeetingDateUtc { get; set; }

        public string? Location { get; set; }

        public MeetingStatus Status { get; set; } = MeetingStatus.Planned;

        /// <summary>
        /// Points to the root Meeting of this version chain. Null means this Meeting IS the root
        /// (i.e. version 1). All meetings sharing the same RootMeetingId (or, for the root
        /// itself, the same Id) belong to one version series.
        /// </summary>
        public int? RootMeetingId { get; set; }

        public int VersionNumber { get; set; } = 1;

        /// <summary>Dagsorden — points to discuss, written before the meeting.</summary>
        public string? AgendaNotes { get; set; }

        /// <summary>Referat — notes written during/after the meeting.</summary>
        public string? MinutesNotes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ICollection<MeetingGroup> Groups { get; set; } = new List<MeetingGroup>();

        public virtual ICollection<MeetingAttendee> Attendees { get; set; } = new List<MeetingAttendee>();

        public virtual ICollection<MeetingDecision> Decisions { get; set; } = new List<MeetingDecision>();

        public virtual ICollection<MeetingAttachment> Attachments { get; set; } = new List<MeetingAttachment>();
    }
}
