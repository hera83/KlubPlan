using web.Repositories.Meetings.Dtos;

namespace web.Repositories.Meetings.Interfaces;

/// <summary>
/// Meetings in version series (RootMeetingId = the first version's id) with groups, attendees,
/// decisions and attachments. Only the latest version of a series can be changed — every write
/// below refuses older versions. Attachment files are FileMetadata rows owned by the Files repository.
/// </summary>
public interface IMeetingRepository
{
    /// <summary>The latest version of each meeting series matching the search, by meeting date (newest first). Page size 10-500, otherwise 10.</summary>
    Task<MeetingPageDto> SearchAsync(SearchMeetingsDto search, CancellationToken cancellationToken = default);

    Task<MeetingDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> IsLatestVersionAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns the new meeting's id.</summary>
    Task<int> CreateAsync(CreateMeetingDto dto, CancellationToken cancellationToken = default);

    Task<SaveMeetingStatus> UpdateAsync(int id, UpdateMeetingDto dto, CancellationToken cancellationToken = default);

    /// <summary>Copies a held/cancelled latest version (title, location, agenda, groups, attendees) into a new planned version on the given date.</summary>
    Task<CreateMeetingVersionResultDto> CreateNewVersionAsync(int sourceMeetingId, DateTime meetingDateUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the meeting (groups, attendees, decisions, attachment rows cascade). Deleting a series'
    /// first version promotes the next one to root. Returns the attachment files to delete, or null when not found.
    /// </summary>
    Task<List<int>?> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> SetAttendanceAsync(int meetingId, string userId, bool hasAttended, CancellationToken cancellationToken = default);

    Task<bool> SaveNotesAsync(int meetingId, string? agendaNotes, string? minutesNotes, CancellationToken cancellationToken = default);

    /// <summary>Null when the meeting isn't found or isn't the latest version.</summary>
    Task<MeetingDecisionDto?> AddDecisionAsync(int meetingId, CreateMeetingDecisionDto dto, CancellationToken cancellationToken = default);

    Task<bool> UpdateDecisionAsync(int decisionId, UpdateMeetingDecisionDto dto, CancellationToken cancellationToken = default);

    Task<bool> SetDecisionCompletionAsync(int decisionId, bool isCompleted, CancellationToken cancellationToken = default);

    Task<bool> DeleteDecisionAsync(int decisionId, CancellationToken cancellationToken = default);

    /// <summary>Attaches a stored file to the meeting. Null when the meeting isn't found or isn't the latest version.</summary>
    Task<MeetingAttachmentDto?> AddAttachmentAsync(int meetingId, int fileId, bool isRecording, CancellationToken cancellationToken = default);

    /// <summary>Removes an attachment row. Returns its stored file id to delete, or null when not found or not the latest version.</summary>
    Task<int?> RemoveAttachmentAsync(int attachmentId, CancellationToken cancellationToken = default);

    /// <summary>The stored file behind an attachment, or null.</summary>
    Task<int?> GetAttachmentFileIdAsync(int attachmentId, CancellationToken cancellationToken = default);

    /// <summary>Marks an audio attachment as queued for transcription (resets any earlier result).</summary>
    Task<RequestTranscriptionStatus> RequestTranscriptionAsync(int attachmentId, CancellationToken cancellationToken = default);

    Task<TranscriptionStatusDto?> GetTranscriptionStatusAsync(int attachmentId, CancellationToken cancellationToken = default);

    /// <summary>Marks the attachment as being transcribed. Null when it no longer exists.</summary>
    Task<TranscriptionJobDto?> StartTranscriptionAsync(int attachmentId, CancellationToken cancellationToken = default);

    /// <summary>Appends the text to the meeting's minutes (as a new paragraph) and marks the transcription completed.</summary>
    Task CompleteTranscriptionAsync(int attachmentId, string text, CancellationToken cancellationToken = default);

    /// <summary>Marks the transcription failed with the error message.</summary>
    Task FailTranscriptionAsync(int attachmentId, string error, CancellationToken cancellationToken = default);
}
