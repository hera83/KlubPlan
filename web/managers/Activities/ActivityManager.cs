using System.Globalization;
using web.Constants;
using web.Managers.Activities.Dtos;
using web.Managers.Activities.Interfaces;
using web.Repositories.Activities.Dtos;
using web.Repositories.Activities.Interfaces;
using web.Repositories.ActivityFiles.Interfaces;
using web.Repositories.Files.Interfaces;
using web.Repositories.Forms.Dtos;
using web.Repositories.Forms.Interfaces;
using web.Repositories.People.Interfaces;
using web.Repositories.PersonGroups.Interfaces;
using web.Repositories.Users.Interfaces;

namespace web.Managers.Activities;

public class ActivityManager(
    IActivityRepository activityRepository,
    IPersonGroupRepository personGroupRepository,
    IPersonRepository personRepository,
    IUserRepository userRepository,
    IFormRepository formRepository,
    IActivityFileRepository activityFileRepository,
    IFileRepository fileRepository) : IActivityManager
{
    private static readonly string[] AdminRoles = [AppRoles.Administrator, AppRoles.Developer];

    public async Task<ActivityListDto> GetListAsync(SearchActivitiesDto search, CancellationToken cancellationToken = default) => new()
    {
        Page = await activityRepository.SearchAsync(search, cancellationToken),
        TotalGroupCount = await personGroupRepository.CountAsync(cancellationToken)
    };

    public async Task<ActivityFormDto?> GetFormAsync(int? id, CancellationToken cancellationToken = default)
    {
        ActivityDto? activity = null;
        if (id.HasValue)
        {
            activity = await activityRepository.GetByIdAsync(id.Value, cancellationToken);
            if (activity is null)
            {
                return null;
            }
        }

        return new ActivityFormDto
        {
            Activity = activity,
            Groups = await personGroupRepository.GetAllAsync(cancellationToken)
        };
    }

    public async Task<ActivityDetailsDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var activity = await activityRepository.GetByIdAsync(id, cancellationToken);
        if (activity is null)
        {
            return null;
        }

        var linkedFormIds = activity.LinkedForms.Select(l => l.FormId).ToHashSet();
        var forms = await formRepository.GetLatestVersionsAsync(acceptingResponsesOnly: false, cancellationToken);

        return new ActivityDetailsDto
        {
            Activity = activity,
            TotalGroupCount = await personGroupRepository.CountAsync(cancellationToken),
            TargetAudienceCount = await personRepository.CountInGroupsAsync(activity.TargetGroups.Select(g => g.Id).ToList(), cancellationToken),
            AdminOptions = await userRepository.GetActiveInRolesAsync(AdminRoles, cancellationToken),
            FormOptions = forms.Where(f => !linkedFormIds.Contains(f.Id)).ToList()
        };
    }

    public async Task<LinkActivityFormStatus> LinkFormAsync(int activityId, int formId, bool createNewVersion, string? userId, CancellationToken cancellationToken = default)
    {
        if (!await activityRepository.ExistsAsync(activityId, cancellationToken))
        {
            return LinkActivityFormStatus.ActivityNotFound;
        }

        var form = await formRepository.GetByIdAsync(formId, cancellationToken);
        if (form is null)
        {
            return LinkActivityFormStatus.FormNotFound;
        }

        var linkFormId = formId;
        if (createNewVersion)
        {
            // A new version requires the source to be closed for responses — the user has already
            // confirmed "opret ny version" in the dialog, so close it here instead of failing.
            if (form.IsAcceptingResponses)
            {
                var closed = await formRepository.SetAcceptingResponsesAsync(formId, false, cancellationToken);
                if (closed.Status == SaveFormStatus.NotLatestVersion)
                {
                    return LinkActivityFormStatus.NotLatestVersion;
                }
            }

            var version = await formRepository.CreateNewVersionAsync(formId, userId, cancellationToken);
            switch (version.Status)
            {
                case CreateFormVersionStatus.NotFound:
                    return LinkActivityFormStatus.FormNotFound;
                case CreateFormVersionStatus.StillAcceptingResponses:
                    return LinkActivityFormStatus.StillAcceptingResponses;
                case CreateFormVersionStatus.NotLatestVersion:
                    return LinkActivityFormStatus.NotLatestVersion;
            }

            linkFormId = version.FormId;
        }

        return await activityRepository.LinkFormAsync(activityId, linkFormId, cancellationToken)
            ? LinkActivityFormStatus.Linked
            : LinkActivityFormStatus.ActivityNotFound;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await activityRepository.ExistsAsync(id, cancellationToken))
        {
            return false;
        }

        // Files go first so the physical files under app_files/activities/<id>/ go too —
        // a DB cascade alone would leave them orphaned on disk.
        foreach (var storedFileId in await activityFileRepository.DeleteAllForActivityAsync(id, cancellationToken))
        {
            await fileRepository.DeleteAsync(storedFileId, cancellationToken);
        }

        await fileRepository.DeleteEmptyFolderAsync(FileCategories.Activities, id.ToString(CultureInfo.InvariantCulture), cancellationToken);

        return await activityRepository.DeleteAsync(id, cancellationToken);
    }
}
