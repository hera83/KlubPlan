using web.Managers.Meetings.Dtos;
using web.Repositories.Files.Dtos;
using web.Repositories.Meetings.Dtos;

namespace web.Managers.Meetings.Interfaces;

/// <summary>Meeting operations across Meetings, Files, PersonGroups, Users, the transcription queue and the AI gateway.</summary>
public interface IMeetingManager
{
    Task<MeetingsOverviewDto> GetOverviewAsync(SearchMeetingsDto search, CancellationToken cancellationToken = default);

    /// <summary>Null when the meeting doesn't exist.</summary>
    Task<MeetingDetailsDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Stores the file under app_files/meetings/ and attaches it. Null when the meeting isn't found or isn't the latest version.</summary>
    Task<MeetingAttachmentDto?> AddAttachmentAsync(int meetingId, Stream content, string originalFileName, string contentType, string? uploaderId, bool isRecording, CancellationToken cancellationToken = default);

    /// <summary>Removes the attachment and deletes its file. False when not found or not the latest version.</summary>
    Task<bool> DeleteAttachmentAsync(int attachmentId, CancellationToken cancellationToken = default);

    Task<StoredFileDto?> GetAttachmentFileAsync(int attachmentId, CancellationToken cancellationToken = default);

    /// <summary>Deletes the meeting and its attachment files. False when not found.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Marks an audio attachment as queued and puts it on the background transcription queue.</summary>
    Task<RequestTranscriptionStatus> RequestTranscriptionAsync(int attachmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The background job (TranscriptionWorker): transcribes the audio via AiGateway/Speaches, has the
    /// chat model clean up the wording, and appends the text to the meeting's minutes. A failure is
    /// stored on the attachment, never thrown.
    /// </summary>
    Task TranscribeAsync(int attachmentId, CancellationToken cancellationToken = default);

    /// <summary>A structured summary of the meeting's agenda and minutes, written by the AI chat model.</summary>
    Task<GenerateSummaryResultDto> GenerateSummaryAsync(int meetingId, CancellationToken cancellationToken = default);
}
