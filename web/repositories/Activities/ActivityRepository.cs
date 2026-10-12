using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Data.Models;
using web.Repositories.Activities.Dtos;
using web.Repositories.Activities.Interfaces;
using web.Repositories.Activities.Mappers;

namespace web.Repositories.Activities;

public class ActivityRepository(WebDbContext dbContext) : IActivityRepository
{
    public async Task<ActivityPageDto> SearchAsync(SearchActivitiesDto search, CancellationToken cancellationToken = default)
    {
        var page = search.Page < 1 ? 1 : search.Page;
        var pageSize = search.PageSize <= 0 ? 10 : search.PageSize;

        var query = dbContext.Activities.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var term = search.SearchText.Trim();
            query = query.Where(a =>
                a.Title.Contains(term) ||
                (a.Location != null && a.Location.Contains(term)) ||
                (a.Category != null && a.Category.Contains(term)));
        }

        return new ActivityPageDto
        {
            TotalCount = await query.CountAsync(cancellationToken),
            Page = page,
            PageSize = pageSize,
            Items = await query
                .OrderByDescending(a => a.StartAtUtc ?? a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ActivityMapper.ToListItemDtoExpression)
                .ToListAsync(cancellationToken)
        };
    }

    public Task<ActivityDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Activities.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(ActivityMapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Activities.AnyAsync(a => a.Id == id, cancellationToken);

    public async Task<SaveActivityResultDto> SaveAsync(SaveActivityDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return new SaveActivityResultDto { Status = SaveActivityStatus.TitleRequired };
        }

        if (dto.StartAtUtc.HasValue && dto.EndAtUtc.HasValue && dto.EndAtUtc.Value <= dto.StartAtUtc.Value)
        {
            return new SaveActivityResultDto { Status = SaveActivityStatus.EndNotAfterStart };
        }

        Activity activity;
        if (dto.Id > 0)
        {
            var existing = await dbContext.Activities
                .Include(a => a.TargetGroups)
                .FirstOrDefaultAsync(a => a.Id == dto.Id, cancellationToken);
            if (existing is null)
            {
                return new SaveActivityResultDto { Status = SaveActivityStatus.NotFound };
            }

            dbContext.ActivityTargetGroups.RemoveRange(existing.TargetGroups);
            existing.TargetGroups.Clear();
            dto.ApplyTo(existing);
            // Touch UpdatedAt even when only the target groups changed.
            dbContext.Entry(existing).State = EntityState.Modified;
            activity = existing;
        }
        else
        {
            activity = dto.ToEntity();
            dbContext.Activities.Add(activity);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new SaveActivityResultDto { Status = SaveActivityStatus.Saved, Id = activity.Id };
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var activity = await dbContext.Activities.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (activity is null)
        {
            return false;
        }

        dbContext.Activities.Remove(activity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<SaveActivityWorkgroupMemberStatus> SaveWorkgroupMemberAsync(SaveActivityWorkgroupMemberDto dto, CancellationToken cancellationToken = default)
    {
        var applicationUserId = string.IsNullOrWhiteSpace(dto.ApplicationUserId) ? null : dto.ApplicationUserId;
        if (applicationUserId is null && string.IsNullOrWhiteSpace(dto.Name))
        {
            return SaveActivityWorkgroupMemberStatus.NameRequired;
        }

        ActivityWorkgroupMember member;
        if (dto.Id > 0)
        {
            var existing = await dbContext.ActivityWorkgroupMembers.FirstOrDefaultAsync(m => m.Id == dto.Id, cancellationToken);
            if (existing is null)
            {
                return SaveActivityWorkgroupMemberStatus.NotFound;
            }

            member = existing;
        }
        else
        {
            if (!await dbContext.Activities.AnyAsync(a => a.Id == dto.ActivityId, cancellationToken))
            {
                return SaveActivityWorkgroupMemberStatus.ActivityNotFound;
            }

            member = new ActivityWorkgroupMember
            {
                ActivityId = dto.ActivityId,
                Order = await dbContext.ActivityWorkgroupMembers.CountAsync(m => m.ActivityId == dto.ActivityId, cancellationToken)
            };
        }

        if (applicationUserId is not null)
        {
            if (!await dbContext.Users.AnyAsync(u => u.Id == applicationUserId, cancellationToken))
            {
                return SaveActivityWorkgroupMemberStatus.UserNotFound;
            }

            if (await dbContext.ActivityWorkgroupMembers.AnyAsync(m => m.ActivityId == member.ActivityId && m.ApplicationUserId == applicationUserId && m.Id != dto.Id, cancellationToken))
            {
                return SaveActivityWorkgroupMemberStatus.AlreadyMember;
            }
        }

        dto.ApplyTo(member);
        if (member.Id == 0)
        {
            dbContext.ActivityWorkgroupMembers.Add(member);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return SaveActivityWorkgroupMemberStatus.Saved;
    }

    public async Task<bool> DeleteWorkgroupMemberAsync(int id, CancellationToken cancellationToken = default)
    {
        var member = await dbContext.ActivityWorkgroupMembers.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (member is null)
        {
            return false;
        }

        // AssignedToWorkgroupMemberId on the member's tasks is SetNull.
        dbContext.ActivityWorkgroupMembers.Remove(member);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<SaveActivityTaskStatus> SaveTaskAsync(SaveActivityTaskDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return SaveActivityTaskStatus.TitleRequired;
        }

        ActivityTask task;
        if (dto.Id > 0)
        {
            var existing = await dbContext.ActivityTasks.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);
            if (existing is null)
            {
                return SaveActivityTaskStatus.NotFound;
            }

            task = existing;
        }
        else
        {
            if (!await dbContext.Activities.AnyAsync(a => a.Id == dto.ActivityId, cancellationToken))
            {
                return SaveActivityTaskStatus.ActivityNotFound;
            }

            task = new ActivityTask
            {
                ActivityId = dto.ActivityId,
                Order = await dbContext.ActivityTasks.CountAsync(t => t.ActivityId == dto.ActivityId, cancellationToken)
            };
        }

        if (dto.AssignedToWorkgroupMemberId.HasValue &&
            !await dbContext.ActivityWorkgroupMembers.AnyAsync(m => m.Id == dto.AssignedToWorkgroupMemberId.Value && m.ActivityId == task.ActivityId, cancellationToken))
        {
            return SaveActivityTaskStatus.AssigneeNotFound;
        }

        dto.ApplyTo(task);
        if (task.Id == 0)
        {
            dbContext.ActivityTasks.Add(task);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return SaveActivityTaskStatus.Saved;
    }

    public async Task<bool> ToggleTaskCompletedAsync(int id, string? userId, CancellationToken cancellationToken = default)
    {
        var task = await dbContext.ActivityTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return false;
        }

        task.IsCompleted = !task.IsCompleted;
        task.CompletedAtUtc = task.IsCompleted ? DateTime.UtcNow : null;
        task.CompletedByUserId = task.IsCompleted ? userId : null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteTaskAsync(int id, CancellationToken cancellationToken = default)
    {
        var task = await dbContext.ActivityTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return false;
        }

        dbContext.ActivityTasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> LinkFormAsync(int activityId, int formId, CancellationToken cancellationToken = default)
    {
        var activity = await dbContext.Activities.FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);
        if (activity is null)
        {
            return false;
        }

        if (!await dbContext.ActivityForms.AnyAsync(l => l.ActivityId == activityId && l.FormId == formId, cancellationToken))
        {
            dbContext.ActivityForms.Add(new ActivityForm { ActivityId = activityId, FormId = formId });
        }

        dbContext.Entry(activity).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnlinkFormAsync(int activityId, int formId, CancellationToken cancellationToken = default)
    {
        var link = await dbContext.ActivityForms.FirstOrDefaultAsync(l => l.ActivityId == activityId && l.FormId == formId, cancellationToken);
        if (link is null)
        {
            return false;
        }

        dbContext.ActivityForms.Remove(link);
        var activity = await dbContext.Activities.FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);
        if (activity is not null)
        {
            dbContext.Entry(activity).State = EntityState.Modified;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
