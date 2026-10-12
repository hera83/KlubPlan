using web.Managers.Activities.Dtos;
using web.Repositories.Activities.Dtos;

namespace web.Managers.Activities.Interfaces;

/// <summary>Activity operations across Activities, PersonGroups, People, Users, Forms, ActivityFiles and Files.</summary>
public interface IActivityManager
{
    /// <summary>The activities table plus the group count for "Alle".</summary>
    Task<ActivityListDto> GetListAsync(SearchActivitiesDto search, CancellationToken cancellationToken = default);

    /// <summary>The create (id null) or edit form with the group picker. Null when the activity doesn't exist.</summary>
    Task<ActivityFormDto?> GetFormAsync(int? id, CancellationToken cancellationToken = default);

    /// <summary>The activity page's own data (Oversigt, Arbejdsgruppe, Opgaver and the form picker). Null when not found.</summary>
    Task<ActivityDetailsDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links a form to the activity. With <paramref name="createNewVersion"/> a fresh, empty version is
    /// created first (closing the source for responses if needed) and that version is linked instead.
    /// </summary>
    Task<LinkActivityFormStatus> LinkFormAsync(int activityId, int formId, bool createNewVersion, string? userId, CancellationToken cancellationToken = default);

    /// <summary>Deletes the activity with its files on disk. False when not found.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
