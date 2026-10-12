using web.Extensions;
using web.Managers.Activities.Dtos;
using web.Repositories.Activities.Dtos;

namespace web.ViewModels.Mappers;

public static class ActivitiesMapper
{
    public static SearchActivitiesDto ToSearchActivitiesDto(this ActivityFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        Page = filter.Page,
        PageSize = filter.PageSize
    };

    public static ActivityFilterViewModel ToActivityFilterViewModel(this ActivityListDto list, string? searchText) => new()
    {
        SearchText = searchText,
        Page = list.Page.Page,
        PageSize = list.Page.PageSize,
        TotalCount = list.Page.TotalCount,
        Aktiviteter = list.Page.Items.Select(a => new ActivityListItemViewModel
        {
            Id = a.Id,
            Title = a.Title,
            Location = a.Location,
            StartAtUtc = a.StartAtUtc,
            EndAtUtc = a.EndAtUtc,
            IsCancelled = a.IsCancelled,
            TargetGroupNames = a.TargetGroupNames.OrderBy(n => n).ToList(),
            IsAllGroups = GroupDisplayHelper.IsAllGroups(a.TargetGroupNames.Count, list.TotalGroupCount),
            TaskTotalCount = a.TaskTotalCount,
            TaskCompletedCount = a.TaskCompletedCount,
            CreatedAtUtc = a.CreatedAt
        }).ToList()
    };

    /// <summary>The create form (no activity) or the edit form, with the group picker.</summary>
    public static ActivityFormViewModel ToActivityFormViewModel(this ActivityFormDto form)
    {
        var activity = form.Activity;
        var model = new ActivityFormViewModel { GroupOptions = form.Groups.ToPersonGroupOptionViewModels() };
        if (activity is null)
        {
            return model;
        }

        model.Id = activity.Id;
        model.Title = activity.Title;
        model.Description = activity.Description;
        model.Location = activity.Location;
        model.Category = activity.Category;
        model.StartAt = activity.StartAtUtc.FromUtcToLocal();
        model.EndAt = activity.EndAtUtc.FromUtcToLocal();
        model.IsCancelled = activity.IsCancelled;
        model.GroupIds = activity.TargetGroups.Select(g => g.Id).ToList();
        return model;
    }

    public static SaveActivityDto ToSaveActivityDto(this ActivityFormViewModel model, string? userId) => new()
    {
        Id = model.Id,
        Title = model.Title,
        Description = model.Description,
        Location = model.Location,
        Category = model.Category,
        StartAtUtc = model.StartAt.FromLocalToUtc(),
        EndAtUtc = model.EndAt.FromLocalToUtc(),
        IsCancelled = model.IsCancelled,
        GroupIds = model.GroupIds,
        CreatedByUserId = userId
    };

    /// <summary>
    /// The activity page's own tabs. LinkedForms get their titles here; the caller fills each form's
    /// responses, plus Messages, ComposeOptions, Files and Lists from their own areas.
    /// </summary>
    public static ActivityDetailsViewModel ToActivityDetailsViewModel(this ActivityDetailsDto details)
    {
        var activity = details.Activity;
        var memberNames = activity.WorkgroupMembers.ToDictionary(m => m.Id, EffectiveMemberName);

        return new ActivityDetailsViewModel
        {
            Id = activity.Id,
            Title = activity.Title,
            Description = activity.Description,
            Location = activity.Location,
            Category = activity.Category,
            StartAtUtc = activity.StartAtUtc,
            EndAtUtc = activity.EndAtUtc,
            IsCancelled = activity.IsCancelled,
            TargetGroups = activity.TargetGroups
                .Select(g => new PersonGroupOptionViewModel { Id = g.Id, Name = g.Name })
                .OrderBy(g => g.Name)
                .ToList(),
            IsAllGroups = GroupDisplayHelper.IsAllGroups(activity.TargetGroups.Count, details.TotalGroupCount),
            TargetAudienceCount = details.TargetAudienceCount,
            WorkgroupMembers = activity.WorkgroupMembers
                .OrderBy(m => m.Order).ThenBy(m => memberNames[m.Id])
                .Select(m => new ActivityWorkgroupMemberViewModel
                {
                    Id = m.Id,
                    ActivityId = m.ActivityId,
                    ApplicationUserId = m.ApplicationUserId,
                    Name = memberNames[m.Id],
                    Role = m.Role,
                    Email = m.Email,
                    Mobile = m.Mobile
                })
                .ToList(),
            AdminOptions = details.AdminOptions.ToMeetingAdminOptionViewModels(),
            Tasks = activity.Tasks
                .OrderBy(t => t.IsCompleted)
                .ThenBy(t => t.DeadlineAtUtc ?? DateTime.MaxValue)
                .ThenBy(t => t.Order)
                .Select(t => new ActivityTaskViewModel
                {
                    Id = t.Id,
                    ActivityId = t.ActivityId,
                    Title = t.Title,
                    DeadlineAtUtc = t.DeadlineAtUtc,
                    AssignedToWorkgroupMemberId = t.AssignedToWorkgroupMemberId,
                    AssignedToName = t.AssignedToWorkgroupMemberId.HasValue && memberNames.TryGetValue(t.AssignedToWorkgroupMemberId.Value, out var name) ? name : null,
                    IsCompleted = t.IsCompleted,
                    Note = t.Note,
                    CompletedAtUtc = t.CompletedAtUtc
                })
                .ToList(),
            LinkedForms = activity.LinkedForms
                .OrderBy(l => l.FormTitle)
                .Select(l => new ActivityLinkedFormViewModel { FormId = l.FormId, FormTitle = l.FormTitle })
                .ToList(),
            FormOptions = details.FormOptions.Select(f => new ActivityFormOptionViewModel { Id = f.Id, Title = f.Title }).ToList()
        };
    }

    public static SaveActivityWorkgroupMemberDto ToSaveActivityWorkgroupMemberDto(this ActivityWorkgroupMemberFormViewModel model) => new()
    {
        Id = model.Id,
        ActivityId = model.ActivityId,
        ApplicationUserId = model.ApplicationUserId,
        Name = model.Name,
        Role = model.Role,
        Email = model.Email,
        Mobile = model.Mobile
    };

    public static SaveActivityTaskDto ToSaveActivityTaskDto(this ActivityTaskFormViewModel model) => new()
    {
        Id = model.Id,
        ActivityId = model.ActivityId,
        Title = model.Title,
        DeadlineAtUtc = model.DeadlineAt.FromLocalToUtc(),
        AssignedToWorkgroupMemberId = model.AssignedToWorkgroupMemberId,
        Note = model.Note
    };

    /// <summary>The linked administrator's account name, or the external contact's own name.</summary>
    private static string EffectiveMemberName(ActivityWorkgroupMemberDto member) =>
        member.ApplicationUserId is not null
            ? member.UserDisplayName ?? "(ukendt bruger)"
            : member.Name ?? string.Empty;
}
