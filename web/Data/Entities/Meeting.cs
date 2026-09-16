using web.Constants;

namespace web.Data.Entities
{
    /// <summary>
    /// A meeting held by the club's administrators. Notes are split into an agenda (written
    /// before the meeting) and minutes (written during/after), since mixing the two is the
    /// classic failure mode of meeting tools. Optionally tagged with the PersonGroup the
    /// meeting concerns (e.g. a specific team), independent of who attended.
    /// </summary>
    public class Meeting
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime MeetingDateUtc { get; set; }

        public string? Location { get; set; }

        public MeetingStatus Status { get; set; } = MeetingStatus.Planned;

        /// <summary>Dagsorden — points to discuss, written before the meeting.</summary>
        public string? AgendaNotes { get; set; }

        /// <summary>Referat — notes written during/after the meeting.</summary>
        public string? MinutesNotes { get; set; }

        /// <summary>Optional: the PersonGroup (team/committee) this meeting concerns.</summary>
        public int? PersonGroupId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public virtual PersonGroup? PersonGroup { get; set; }

        public virtual ICollection<MeetingAttendee> Attendees { get; set; } = new List<MeetingAttendee>();

        public virtual ICollection<MeetingDecision> Decisions { get; set; } = new List<MeetingDecision>();

        public virtual ICollection<MeetingAttachment> Attachments { get; set; } = new List<MeetingAttachment>();
    }
}
