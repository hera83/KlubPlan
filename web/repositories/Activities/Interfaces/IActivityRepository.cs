using web.Repositories.Activities.Dtos;

namespace web.Repositories.Activities.Interfaces;

/// <summary>Activities with their target groups, arbejdsgruppe, tasks and links to forms.</summary>
public interface IActivityRepository
{
    /// <summary>The table page matching the search, by start (or creation) newest first. Page size 10 when not set.</summary>
    Task<ActivityPageDto> SearchAsync(SearchActivitiesDto search, CancellationToken cancellationToken = default);

    Task<ActivityDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates (Id = 0) or updates the activity and replaces its target groups.</summary>
    Task<SaveActivityResultDto> SaveAsync(SaveActivityDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the activity; target groups, arbejdsgruppe, tasks, form links and work lists cascade.
    /// The forms and messages themselves are kept. Files must be deleted first (see IActivityManager).
    /// </summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates (Id = 0) or updates an arbejdsgruppe member. New members go last.</summary>
    Task<SaveActivityWorkgroupMemberStatus> SaveWorkgroupMemberAsync(SaveActivityWorkgroupMemberDto dto, CancellationToken cancellationToken = default);

    /// <summary>Tasks assigned to the member are kept, unassigned.</summary>
    Task<bool> DeleteWorkgroupMemberAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates (Id = 0) or updates a task. New tasks go last.</summary>
    Task<SaveActivityTaskStatus> SaveTaskAsync(SaveActivityTaskDto dto, CancellationToken cancellationToken = default);

    /// <summary>Flips IsCompleted and records who/when. False when not found.</summary>
    Task<bool> ToggleTaskCompletedAsync(int id, string? userId, CancellationToken cancellationToken = default);

    Task<bool> DeleteTaskAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Links the form (if it isn't already). False when the activity doesn't exist.</summary>
    Task<bool> LinkFormAsync(int activityId, int formId, CancellationToken cancellationToken = default);

    /// <summary>Removes the link — the form itself is kept. False when it isn't linked.</summary>
    Task<bool> UnlinkFormAsync(int activityId, int formId, CancellationToken cancellationToken = default);
}
