using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.Activities.Dtos;

namespace web.Repositories.Activities.Mappers;

public static class ActivityMapper
{
    public static readonly Expression<Func<Activity, ActivityDto>> ToDtoExpression = x => new ActivityDto
    {
        Id = x.Id,
        Title = x.Title,
        Description = x.Description,
        Location = x.Location,
        Category = x.Category,
        StartAtUtc = x.StartAtUtc,
        EndAtUtc = x.EndAtUtc,
        IsCancelled = x.IsCancelled,
        TargetGroups = x.TargetGroups.Select(g => new ActivityTargetGroupDto { Id = g.PersonGroupId, Name = g.PersonGroup.Name }).ToList(),
        WorkgroupMembers = x.WorkgroupMembers.Select(m => new ActivityWorkgroupMemberDto
        {
            Id = m.Id,
            ActivityId = m.ActivityId,
            Order = m.Order,
            ApplicationUserId = m.ApplicationUserId,
            UserDisplayName = m.ApplicationUser != null ? m.ApplicationUser.DisplayName : null,
            Name = m.Name,
            Role = m.Role,
            Email = m.ApplicationUserId != null ? (m.ApplicationUser != null ? m.ApplicationUser.Email : null) : m.Email,
            Mobile = m.ApplicationUserId != null ? (m.ApplicationUser != null ? m.ApplicationUser.PhoneNumber : null) : m.Mobile
        }).ToList(),
        Tasks = x.Tasks.Select(t => new ActivityTaskDto
        {
            Id = t.Id,
            ActivityId = t.ActivityId,
            Title = t.Title,
            DeadlineAtUtc = t.DeadlineAtUtc,
            AssignedToWorkgroupMemberId = t.AssignedToWorkgroupMemberId,
            IsCompleted = t.IsCompleted,
            Note = t.Note,
            CompletedAtUtc = t.CompletedAtUtc,
            Order = t.Order
        }).ToList(),
        LinkedForms = x.LinkedForms.Select(l => new ActivityLinkedFormDto { FormId = l.FormId, FormTitle = l.Form.Title }).ToList()
    };

    public static readonly Expression<Func<Activity, ActivityListItemDto>> ToListItemDtoExpression = x => new ActivityListItemDto
    {
        Id = x.Id,
        Title = x.Title,
        Location = x.Location,
        StartAtUtc = x.StartAtUtc,
        EndAtUtc = x.EndAtUtc,
        IsCancelled = x.IsCancelled,
        TargetGroupNames = x.TargetGroups.Select(g => g.PersonGroup.Name).ToList(),
        TaskTotalCount = x.Tasks.Count,
        TaskCompletedCount = x.Tasks.Count(t => t.IsCompleted),
        CreatedAt = x.CreatedAt
    };

    public static Activity ToEntity(this SaveActivityDto dto)
    {
        var entity = new Activity { CreatedByUserId = dto.CreatedByUserId };
        dto.ApplyTo(entity);
        return entity;
    }

    /// <summary>Sets the fields (trimmed, blank = null) and adds the target groups (the caller removes the old ones first).</summary>
    public static void ApplyTo(this SaveActivityDto dto, Activity entity)
    {
        entity.Title = dto.Title?.Trim() ?? string.Empty;
        entity.Description = NullIfBlank(dto.Description);
        entity.Location = NullIfBlank(dto.Location);
        entity.Category = NullIfBlank(dto.Category);
        entity.StartAtUtc = dto.StartAtUtc;
        entity.EndAtUtc = dto.EndAtUtc;
        entity.IsCancelled = dto.IsCancelled;

        foreach (var groupId in dto.GroupIds.Distinct())
        {
            entity.TargetGroups.Add(new ActivityTargetGroup { PersonGroupId = groupId });
        }
    }

    /// <summary>A linked administrator clears the external contact fields, and vice versa.</summary>
    public static void ApplyTo(this SaveActivityWorkgroupMemberDto dto, ActivityWorkgroupMember entity)
    {
        var applicationUserId = NullIfBlank(dto.ApplicationUserId);
        entity.ApplicationUserId = applicationUserId;
        entity.Name = applicationUserId is null ? dto.Name?.Trim() ?? string.Empty : null;
        entity.Email = applicationUserId is null ? NullIfBlank(dto.Email) : null;
        entity.Mobile = applicationUserId is null ? NullIfBlank(dto.Mobile) : null;
        entity.Role = NullIfBlank(dto.Role);
    }

    public static void ApplyTo(this SaveActivityTaskDto dto, ActivityTask entity)
    {
        entity.Title = dto.Title?.Trim() ?? string.Empty;
        entity.DeadlineAtUtc = dto.DeadlineAtUtc;
        entity.AssignedToWorkgroupMemberId = dto.AssignedToWorkgroupMemberId;
        entity.Note = NullIfBlank(dto.Note);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
