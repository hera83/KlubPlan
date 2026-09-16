using System.ComponentModel.DataAnnotations;
using web.Constants;

namespace web.ViewModels
{
    public class MeetingFilterViewModel
    {
        public string? SearchText { get; set; }
        public MeetingStatus? Status { get; set; }
        public int? PersonGroupId { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public List<MeetingListItemViewModel> Meetings { get; set; } = new();
        public int TotalCount { get; set; }

        /// <summary>All groups, for the filter dropdown.</summary>
        public List<PersonGroupOptionViewModel> Groups { get; set; } = new();
    }

    public class MeetingListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime MeetingDateUtc { get; set; }
        public string? Location { get; set; }
        public MeetingStatus Status { get; set; }
        public string? PersonGroupName { get; set; }
        public int AttendeeCount { get; set; }
        public int AttendedCount { get; set; }
    }

    public class MeetingAdminOptionViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public class MeetingAttendeeViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool HasAttended { get; set; }
    }

    public class MeetingDecisionViewModel
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ResponsibleUserId { get; set; }
        public string? ResponsibleUserName { get; set; }
        public DateOnly? DueDate { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class MeetingAttachmentViewModel
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public bool IsRecording { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class MeetingDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime MeetingDateUtc { get; set; }
        public string? Location { get; set; }
        public MeetingStatus Status { get; set; }
        public int? PersonGroupId { get; set; }
        public string? PersonGroupName { get; set; }
        public string? AgendaNotes { get; set; }
        public string? MinutesNotes { get; set; }

        public List<MeetingAttendeeViewModel> Attendees { get; set; } = new();
        public List<MeetingDecisionViewModel> Decisions { get; set; } = new();
        public List<MeetingAttachmentViewModel> Attachments { get; set; } = new();
    }

    public class CreateMeetingViewModel
    {
        [Required(ErrorMessage = "Titel er påkrævet")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Dato er påkrævet")]
        public DateTime MeetingDate { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        public int? PersonGroupId { get; set; }

        public List<string> AttendeeUserIds { get; set; } = new();

        public string? AgendaNotes { get; set; }
    }

    public class EditMeetingViewModel : CreateMeetingViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public MeetingStatus Status { get; set; }
    }

    public class SaveMeetingNotesViewModel
    {
        [Required]
        public int MeetingId { get; set; }

        public string? AgendaNotes { get; set; }

        public string? MinutesNotes { get; set; }
    }

    public class SetMeetingAttendanceViewModel
    {
        [Required]
        public int MeetingId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public bool HasAttended { get; set; }
    }

    public class CreateMeetingDecisionViewModel
    {
        [Required]
        public int MeetingId { get; set; }

        [Required(ErrorMessage = "Beskrivelse er påkrævet")]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public string? ResponsibleUserId { get; set; }

        public DateOnly? DueDate { get; set; }
    }

    public class EditMeetingDecisionViewModel : CreateMeetingDecisionViewModel
    {
        [Required]
        public int Id { get; set; }

        public bool IsCompleted { get; set; }
    }

    public class UploadMeetingAttachmentViewModel
    {
        [Required]
        public int MeetingId { get; set; }

        [Required(ErrorMessage = "Vælg en fil")]
        public IFormFile? File { get; set; }
    }

    public class TranscribeMeetingAudioViewModel
    {
        [Required]
        public int MeetingId { get; set; }

        [Required(ErrorMessage = "Ingen lydoptagelse modtaget")]
        public IFormFile? Audio { get; set; }
    }
}
