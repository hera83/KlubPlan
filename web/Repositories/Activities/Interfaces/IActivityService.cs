using web.Repositories.Activities.Dtos;
using web.ViewModels;

namespace web.Repositories.Activities.Interfaces
{
    public interface IActivityService
    {
        Task<ActivityFilterViewModel> GetActivitiesAsync(ActivityFilterViewModel filter, CancellationToken ct = default);

        /// <summary>Empty form view model with GroupOptions populated, for Create().</summary>
        Task<ActivityFormViewModel> GetFormShellAsync(CancellationToken ct = default);

        Task<ActivityFormViewModel?> GetActivityForEditAsync(int id, CancellationToken ct = default);

        Task<SaveActivityResponseDto> SaveActivityAsync(SaveActivityRequestDto dto, CancellationToken ct = default);

        Task<bool> DeleteActivityAsync(int id, CancellationToken ct = default);

        Task<ActivityDetailsViewModel?> GetActivityDetailsAsync(int id, CancellationToken ct = default);

        Task<ActivityActionResultDto> SaveWorkgroupMemberAsync(SaveWorkgroupMemberRequestDto dto, CancellationToken ct = default);

        Task<bool> DeleteWorkgroupMemberAsync(int id, CancellationToken ct = default);

        Task<ActivityActionResultDto> SaveTaskAsync(SaveTaskRequestDto dto, CancellationToken ct = default);

        Task<ActivityActionResultDto> ToggleTaskCompletedAsync(int id, string? note, string? userId, CancellationToken ct = default);

        Task<bool> DeleteTaskAsync(int id, CancellationToken ct = default);

        /// <summary>Whether the given form already has responses — used to decide if linking it needs the "ny version eller eksisterende" confirmation.</summary>
        Task<FormResponseCheckDto> CheckFormResponsesAsync(int formId, CancellationToken ct = default);

        /// <summary>
        /// Sets/clears the activity's linked Form (formId null = fjern link). When createNewVersion
        /// is true, a fresh blank version of the form is created first (closing the source version
        /// for responses if it's still open) and the activity is linked to that new version instead.
        /// </summary>
        Task<ActivityActionResultDto> LinkFormAsync(int activityId, int? formId, bool createNewVersion, string? userId, CancellationToken ct = default);
    }
}
