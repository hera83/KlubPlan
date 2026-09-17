using web.Repositories.Meetings.Dtos;
using web.ViewModels;

namespace web.Repositories.Meetings.Interfaces
{
    public interface IMeetingsService
    {
        Task<MeetingFilterViewModel> GetMeetingsAsync(MeetingFilterViewModel filter, CancellationToken ct = default);

        Task<MeetingDetailViewModel?> GetMeetingDetailAsync(int id, CancellationToken ct = default);

        Task<List<MeetingAdminOptionViewModel>> GetAdminOptionsAsync(CancellationToken ct = default);

        Task<List<PersonGroupOptionViewModel>> GetGroupOptionsAsync(CancellationToken ct = default);

        Task<CreateMeetingResponseDto> CreateMeetingAsync(CreateMeetingRequestDto dto, CancellationToken ct = default);

        Task<UpdateMeetingResponseDto> UpdateMeetingAsync(UpdateMeetingRequestDto dto, CancellationToken ct = default);

        Task<CreateMeetingResponseDto> CreateNewVersionAsync(int sourceMeetingId, DateTime meetingDateUtc, CancellationToken ct = default);

        Task<bool> DeleteMeetingAsync(int id, CancellationToken ct = default);

        Task<bool> SetAttendanceAsync(int meetingId, string userId, bool hasAttended, CancellationToken ct = default);

        Task<bool> SaveNotesAsync(int meetingId, string? agendaNotes, string? minutesNotes, CancellationToken ct = default);

        Task<MeetingDecisionViewModel?> AddDecisionAsync(int meetingId, string description, string? responsibleUserId, DateOnly? dueDate, CancellationToken ct = default);

        Task<bool> UpdateDecisionAsync(int decisionId, string description, string? responsibleUserId, DateOnly? dueDate, bool isCompleted, CancellationToken ct = default);

        Task<bool> SetDecisionCompletionAsync(int decisionId, bool isCompleted, CancellationToken ct = default);

        Task<bool> DeleteDecisionAsync(int decisionId, CancellationToken ct = default);

        Task<MeetingAttachmentViewModel?> AddAttachmentAsync(int meetingId, Stream fileStream, string originalFileName, string contentType, string? uploaderId, bool isRecording, CancellationToken ct = default);

        Task<bool> DeleteAttachmentAsync(int attachmentId, CancellationToken ct = default);

        Task<(byte[] Data, string ContentType, string FileName)?> GetAttachmentFileAsync(int attachmentId, CancellationToken ct = default);

        Task<(bool Success, string? ErrorMessage)> RequestTranscriptionAsync(int attachmentId, CancellationToken ct = default);

        Task<TranscriptionStatusViewModel?> GetTranscriptionStatusAsync(int attachmentId, CancellationToken ct = default);
    }
}
