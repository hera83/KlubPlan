using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Repositories.Activities.Dtos;
using web.Repositories.Activities.Interfaces;
using web.Repositories.Communication.Interfaces;
using web.Repositories.Forms.Interfaces;
using web.ViewModels;

namespace web.Repositories.Activities
{
    public class ActivityService : IActivityService
    {
        private static readonly string[] AdminRoles = { AppRoles.Administrator, AppRoles.Developer };

        private readonly ApplicationDbContext _context;
        private readonly ICommunicationService _communicationService;
        private readonly IFormService _formService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivityService(ApplicationDbContext context, ICommunicationService communicationService, IFormService formService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _communicationService = communicationService;
            _formService = formService;
            _userManager = userManager;
        }

        public async Task<ActivityFilterViewModel> GetActivitiesAsync(ActivityFilterViewModel filter, CancellationToken ct = default)
        {
            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

            var query = _context.Activities.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var term = filter.SearchText.Trim();
                query = query.Where(a =>
                    a.Title.Contains(term) ||
                    (a.Location != null && a.Location.Contains(term)) ||
                    (a.Category != null && a.Category.Contains(term)));
            }

            var totalCount = await query.CountAsync(ct);
            var totalGroupCount = await _context.PersonGroups.CountAsync(ct);

            var rows = await query
                .OrderByDescending(a => a.StartAtUtc ?? a.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Location,
                    a.StartAtUtc,
                    a.EndAtUtc,
                    a.IsCancelled,
                    a.CreatedAtUtc,
                    GroupNames = a.TargetGroups.Select(g => g.PersonGroup.Name).ToList(),
                    TaskTotalCount = a.Tasks.Count,
                    TaskCompletedCount = a.Tasks.Count(t => t.IsCompleted)
                })
                .ToListAsync(ct);

            return new ActivityFilterViewModel
            {
                SearchText = filter.SearchText,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Aktiviteter = rows.Select(r => new ActivityListItemViewModel
                {
                    Id = r.Id,
                    Title = r.Title,
                    Location = r.Location,
                    StartAtUtc = r.StartAtUtc,
                    EndAtUtc = r.EndAtUtc,
                    IsCancelled = r.IsCancelled,
                    TargetGroupNames = r.GroupNames.OrderBy(n => n).ToList(),
                    IsAllGroups = totalGroupCount > 0 && r.GroupNames.Count == totalGroupCount,
                    TaskTotalCount = r.TaskTotalCount,
                    TaskCompletedCount = r.TaskCompletedCount,
                    CreatedAtUtc = r.CreatedAtUtc
                }).ToList()
            };
        }

        public async Task<ActivityFormViewModel> GetFormShellAsync(CancellationToken ct = default)
        {
            return new ActivityFormViewModel
            {
                GroupOptions = await GetGroupOptionsAsync(ct)
            };
        }

        public async Task<ActivityFormViewModel?> GetActivityForEditAsync(int id, CancellationToken ct = default)
        {
            var activity = await _context.Activities
                .AsNoTracking()
                .Include(a => a.TargetGroups)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (activity is null)
            {
                return null;
            }

            return new ActivityFormViewModel
            {
                Id = activity.Id,
                Title = activity.Title,
                Description = activity.Description,
                Location = activity.Location,
                Category = activity.Category,
                StartAt = ToLocal(activity.StartAtUtc),
                EndAt = ToLocal(activity.EndAtUtc),
                IsCancelled = activity.IsCancelled,
                GroupIds = activity.TargetGroups.Select(g => g.PersonGroupId).ToList(),
                GroupOptions = await GetGroupOptionsAsync(ct)
            };
        }

        public async Task<SaveActivityResponseDto> SaveActivityAsync(SaveActivityRequestDto dto, CancellationToken ct = default)
        {
            var title = dto.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                return new SaveActivityResponseDto { Success = false, ErrorMessage = "Aktiviteten skal have en titel." };
            }

            if (dto.StartAt.HasValue && dto.EndAt.HasValue && dto.EndAt.Value <= dto.StartAt.Value)
            {
                return new SaveActivityResponseDto { Success = false, ErrorMessage = "Sluttidspunkt skal være efter starttidspunkt." };
            }

            Activity activity;
            if (dto.Id > 0)
            {
                var existing = await _context.Activities
                    .Include(a => a.TargetGroups)
                    .FirstOrDefaultAsync(a => a.Id == dto.Id, ct);

                if (existing is null)
                {
                    return new SaveActivityResponseDto { Success = false, ErrorMessage = "Aktiviteten blev ikke fundet." };
                }

                activity = existing;
                _context.ActivityTargetGroups.RemoveRange(existing.TargetGroups);
                existing.TargetGroups.Clear();
                activity.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                activity = new Activity
                {
                    CreatedByUserId = dto.UserId,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _context.Activities.Add(activity);
            }

            activity.Title = title;
            activity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            activity.Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
            activity.Category = string.IsNullOrWhiteSpace(dto.Category) ? null : dto.Category.Trim();
            activity.StartAtUtc = ToUtc(dto.StartAt);
            activity.EndAtUtc = ToUtc(dto.EndAt);
            activity.IsCancelled = dto.IsCancelled;

            foreach (var groupId in dto.GroupIds.Distinct())
            {
                activity.TargetGroups.Add(new ActivityTargetGroup { PersonGroupId = groupId });
            }

            await _context.SaveChangesAsync(ct);

            return new SaveActivityResponseDto { Success = true, ActivityId = activity.Id };
        }

        public async Task<bool> DeleteActivityAsync(int id, CancellationToken ct = default)
        {
            var activity = await _context.Activities.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (activity is null)
            {
                return false;
            }

            // TargetGroups/WorkgroupMembers/Tasks cascade-delete with the activity; a linked
            // Form/CommunicationMessage keeps existing — only their FK back to this activity is
            // cleared (SetNull), same as when a Form itself is deleted.
            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<ActivityDetailsViewModel?> GetActivityDetailsAsync(int id, CancellationToken ct = default)
        {
            var activity = await _context.Activities
                .AsNoTracking()
                .Include(a => a.TargetGroups).ThenInclude(g => g.PersonGroup)
                .Include(a => a.WorkgroupMembers).ThenInclude(m => m.ApplicationUser)
                .Include(a => a.Tasks).ThenInclude(t => t.AssignedToWorkgroupMember).ThenInclude(m => m!.ApplicationUser)
                .Include(a => a.Form)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (activity is null)
            {
                return null;
            }

            var formResponses = activity.FormId.HasValue
                ? await _formService.GetResponsesAsync(activity.FormId.Value, page: 1, pageSize: 10, ct)
                : null;

            var targetGroupIds = activity.TargetGroups.Select(g => g.PersonGroupId).ToList();
            var targetAudienceCount = targetGroupIds.Count > 0
                ? await _context.People.CountAsync(p => p.Memberships.Any(m => targetGroupIds.Contains(m.GroupId)), ct)
                : 0;
            var totalGroupCount = await _context.PersonGroups.CountAsync(ct);

            var composeOptions = await _communicationService.GetComposeOptionsAsync(ct);
            var messages = await _communicationService.GetMessagesForActivityAsync(id, ct);
            var adminOptions = await GetAdminOptionsAsync(ct);

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
                    .Select(g => new PersonGroupOptionViewModel { Id = g.PersonGroupId, Name = g.PersonGroup.Name })
                    .OrderBy(g => g.Name)
                    .ToList(),
                IsAllGroups = totalGroupCount > 0 && targetGroupIds.Count == totalGroupCount,
                TargetAudienceCount = targetAudienceCount,
                WorkgroupMembers = activity.WorkgroupMembers
                    .OrderBy(m => m.Order).ThenBy(m => EffectiveMemberName(m))
                    .Select(m => new ActivityWorkgroupMemberViewModel
                    {
                        Id = m.Id,
                        ActivityId = m.ActivityId,
                        ApplicationUserId = m.ApplicationUserId,
                        Name = EffectiveMemberName(m),
                        Role = m.Role,
                        Email = m.ApplicationUserId is not null ? m.ApplicationUser?.Email : m.Email,
                        Mobile = m.ApplicationUserId is not null ? m.ApplicationUser?.PhoneNumber : m.Mobile
                    })
                    .ToList(),
                AdminOptions = adminOptions,
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
                        AssignedToName = t.AssignedToWorkgroupMember is null ? null : EffectiveMemberName(t.AssignedToWorkgroupMember),
                        IsCompleted = t.IsCompleted,
                        Note = t.Note,
                        CompletedAtUtc = t.CompletedAtUtc
                    })
                    .ToList(),
                FormId = activity.FormId,
                FormTitle = activity.Form?.Title,
                FormOptions = await GetFormOptionsAsync(ct),
                FormResponses = formResponses,
                Messages = messages,
                ComposeOptions = composeOptions
            };
        }

        public async Task<ActivityActionResultDto> SaveWorkgroupMemberAsync(SaveWorkgroupMemberRequestDto dto, CancellationToken ct = default)
        {
            var applicationUserId = string.IsNullOrWhiteSpace(dto.ApplicationUserId) ? null : dto.ApplicationUserId;
            var name = dto.Name?.Trim() ?? string.Empty;

            if (applicationUserId is null && string.IsNullOrWhiteSpace(name))
            {
                return new ActivityActionResultDto { Success = false, ErrorMessage = "Vælg en administrator, eller udfyld navn på den eksterne kontakt." };
            }

            ActivityWorkgroupMember member;
            int activityId;
            if (dto.Id > 0)
            {
                var existing = await _context.ActivityWorkgroupMembers.FirstOrDefaultAsync(m => m.Id == dto.Id, ct);
                if (existing is null)
                {
                    return new ActivityActionResultDto { Success = false, ErrorMessage = "Medlemmet blev ikke fundet." };
                }

                member = existing;
                activityId = existing.ActivityId;
            }
            else
            {
                if (!await _context.Activities.AnyAsync(a => a.Id == dto.ActivityId, ct))
                {
                    return new ActivityActionResultDto { Success = false, ErrorMessage = "Aktiviteten blev ikke fundet." };
                }

                activityId = dto.ActivityId;
                member = new ActivityWorkgroupMember
                {
                    ActivityId = dto.ActivityId,
                    CreatedAtUtc = DateTime.UtcNow,
                    Order = await _context.ActivityWorkgroupMembers.CountAsync(m => m.ActivityId == dto.ActivityId, ct)
                };
                _context.ActivityWorkgroupMembers.Add(member);
            }

            if (applicationUserId is not null)
            {
                if (!await _context.Users.AnyAsync(u => u.Id == applicationUserId, ct))
                {
                    return new ActivityActionResultDto { Success = false, ErrorMessage = "Administratoren blev ikke fundet." };
                }

                var alreadyMember = await _context.ActivityWorkgroupMembers
                    .AnyAsync(m => m.ActivityId == activityId && m.ApplicationUserId == applicationUserId && m.Id != dto.Id, ct);
                if (alreadyMember)
                {
                    return new ActivityActionResultDto { Success = false, ErrorMessage = "Denne administrator er allerede en del af arbejdsgruppen." };
                }

                member.ApplicationUserId = applicationUserId;
                member.Name = null;
                member.Email = null;
                member.Mobile = null;
            }
            else
            {
                member.ApplicationUserId = null;
                member.Name = name;
                member.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
                member.Mobile = string.IsNullOrWhiteSpace(dto.Mobile) ? null : dto.Mobile.Trim();
            }

            member.Role = string.IsNullOrWhiteSpace(dto.Role) ? null : dto.Role.Trim();

            await _context.SaveChangesAsync(ct);
            return new ActivityActionResultDto { Success = true };
        }

        public async Task<bool> DeleteWorkgroupMemberAsync(int id, CancellationToken ct = default)
        {
            var member = await _context.ActivityWorkgroupMembers.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (member is null)
            {
                return false;
            }

            // Tasks assigned to this member keep existing — AssignedToWorkgroupMemberId is SetNull.
            _context.ActivityWorkgroupMembers.Remove(member);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<ActivityActionResultDto> SaveTaskAsync(SaveTaskRequestDto dto, CancellationToken ct = default)
        {
            var title = dto.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                return new ActivityActionResultDto { Success = false, ErrorMessage = "Opgaven skal have en titel." };
            }

            ActivityTask task;
            int activityId;
            if (dto.Id > 0)
            {
                var existing = await _context.ActivityTasks.FirstOrDefaultAsync(t => t.Id == dto.Id, ct);
                if (existing is null)
                {
                    return new ActivityActionResultDto { Success = false, ErrorMessage = "Opgaven blev ikke fundet." };
                }

                task = existing;
                activityId = existing.ActivityId;
            }
            else
            {
                if (!await _context.Activities.AnyAsync(a => a.Id == dto.ActivityId, ct))
                {
                    return new ActivityActionResultDto { Success = false, ErrorMessage = "Aktiviteten blev ikke fundet." };
                }

                activityId = dto.ActivityId;
                task = new ActivityTask
                {
                    ActivityId = dto.ActivityId,
                    CreatedAtUtc = DateTime.UtcNow,
                    Order = await _context.ActivityTasks.CountAsync(t => t.ActivityId == dto.ActivityId, ct)
                };
                _context.ActivityTasks.Add(task);
            }

            if (dto.AssignedToWorkgroupMemberId.HasValue &&
                !await _context.ActivityWorkgroupMembers.AnyAsync(m => m.Id == dto.AssignedToWorkgroupMemberId.Value && m.ActivityId == activityId, ct))
            {
                return new ActivityActionResultDto { Success = false, ErrorMessage = "Den valgte ansvarlige findes ikke på denne aktivitet." };
            }

            task.Title = title;
            task.DeadlineAtUtc = ToUtc(dto.DeadlineAt);
            task.AssignedToWorkgroupMemberId = dto.AssignedToWorkgroupMemberId;
            task.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();

            await _context.SaveChangesAsync(ct);
            return new ActivityActionResultDto { Success = true };
        }

        public async Task<ActivityActionResultDto> ToggleTaskCompletedAsync(int id, string? note, string? userId, CancellationToken ct = default)
        {
            var task = await _context.ActivityTasks.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (task is null)
            {
                return new ActivityActionResultDto { Success = false, ErrorMessage = "Opgaven blev ikke fundet." };
            }

            task.IsCompleted = !task.IsCompleted;
            if (task.IsCompleted)
            {
                task.CompletedAtUtc = DateTime.UtcNow;
                task.CompletedByUserId = userId;
            }
            else
            {
                task.CompletedAtUtc = null;
                task.CompletedByUserId = null;
            }

            if (!string.IsNullOrWhiteSpace(note))
            {
                task.Note = note.Trim();
            }

            await _context.SaveChangesAsync(ct);
            return new ActivityActionResultDto { Success = true };
        }

        public async Task<bool> DeleteTaskAsync(int id, CancellationToken ct = default)
        {
            var task = await _context.ActivityTasks.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (task is null)
            {
                return false;
            }

            _context.ActivityTasks.Remove(task);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<FormResponseCheckDto> CheckFormResponsesAsync(int formId, CancellationToken ct = default)
        {
            var responses = await _formService.GetResponsesAsync(formId, page: 1, pageSize: 1, ct);
            var count = responses?.TotalCount ?? 0;
            return new FormResponseCheckDto { HasResponses = count > 0, ResponseCount = count };
        }

        public async Task<ActivityActionResultDto> LinkFormAsync(int activityId, int? formId, bool createNewVersion, string? userId, CancellationToken ct = default)
        {
            var activity = await _context.Activities.FirstOrDefaultAsync(a => a.Id == activityId, ct);
            if (activity is null)
            {
                return new ActivityActionResultDto { Success = false, ErrorMessage = "Aktiviteten blev ikke fundet." };
            }

            if (!formId.HasValue)
            {
                activity.FormId = null;
                activity.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
                return new ActivityActionResultDto { Success = true };
            }

            var resolvedFormId = formId.Value;

            if (createNewVersion)
            {
                // CreateNewVersionAsync requires the source form to be closed for responses — the
                // user has already confirmed "opret ny version" in the dialog, which makes this
                // explicit, so close it here rather than blocking with an error.
                var sourceForm = await _context.Forms.FirstOrDefaultAsync(f => f.Id == formId.Value, ct);
                if (sourceForm is null)
                {
                    return new ActivityActionResultDto { Success = false, ErrorMessage = "Formularen blev ikke fundet." };
                }

                if (sourceForm.IsAcceptingResponses)
                {
                    sourceForm.IsAcceptingResponses = false;
                    sourceForm.UpdatedAtUtc = DateTime.UtcNow;
                    await _context.SaveChangesAsync(ct);
                }

                var versionResult = await _formService.CreateNewVersionAsync(formId.Value, userId, ct);
                if (!versionResult.Success)
                {
                    return new ActivityActionResultDto { Success = false, ErrorMessage = versionResult.ErrorMessage ?? "Kunne ikke oprette ny version af formularen." };
                }

                resolvedFormId = versionResult.FormId;
            }
            else if (!await _context.Forms.AnyAsync(f => f.Id == resolvedFormId, ct))
            {
                return new ActivityActionResultDto { Success = false, ErrorMessage = "Formularen blev ikke fundet." };
            }

            activity.FormId = resolvedFormId;
            activity.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return new ActivityActionResultDto { Success = true };
        }

        private async Task<List<PersonGroupOptionViewModel>> GetGroupOptionsAsync(CancellationToken ct)
        {
            return await _context.PersonGroups
                .OrderBy(g => g.Name)
                .Select(g => new PersonGroupOptionViewModel { Id = g.Id, Name = g.Name })
                .ToListAsync(ct);
        }

        private async Task<List<ActivityFormOptionViewModel>> GetFormOptionsAsync(CancellationToken ct)
        {
            var forms = await _context.Forms.AsNoTracking().ToListAsync(ct);
            return forms
                .GroupBy(f => f.RootFormId ?? f.Id)
                .Select(g => g.OrderByDescending(f => f.VersionNumber).First())
                .OrderBy(f => f.Title)
                .Select(f => new ActivityFormOptionViewModel { Id = f.Id, Title = f.Title })
                .ToList();
        }

        /// <summary>Active Administrator/Developer users — the pool of accounts arbejdsgruppe members can be linked to. Mirrors MeetingsService.GetAdminOptionsAsync.</summary>
        private async Task<List<MeetingAdminOptionViewModel>> GetAdminOptionsAsync(CancellationToken ct)
        {
            var users = await _context.Users.AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.DisplayName)
                .ToListAsync(ct);

            var options = new List<MeetingAdminOptionViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Any(r => AdminRoles.Contains(r)))
                {
                    options.Add(new MeetingAdminOptionViewModel { Id = user.Id, DisplayName = user.DisplayName });
                }
            }

            return options;
        }

        /// <summary>The name to display for a workgroup member — the linked administrator's account name, or the external contact's own name.</summary>
        private static string EffectiveMemberName(ActivityWorkgroupMember member)
            => member.ApplicationUserId is not null
                ? member.ApplicationUser?.DisplayName ?? "(ukendt bruger)"
                : member.Name ?? string.Empty;

        /// <summary>Converts a datetime-local form value (Unspecified kind, interpreted as browser-local) to UTC for storage.</summary>
        private static DateTime? ToUtc(DateTime? localValue)
            => localValue.HasValue ? DateTime.SpecifyKind(localValue.Value, DateTimeKind.Local).ToUniversalTime() : null;

        /// <summary>Modstykket til ToUtc — SQLite giver DateTime tilbage uden Kind, så den skal markeres som Utc, før ToLocalTime() konverterer korrekt.</summary>
        private static DateTime? ToLocal(DateTime? utcValue)
            => utcValue.HasValue ? DateTime.SpecifyKind(utcValue.Value, DateTimeKind.Utc).ToLocalTime() : null;
    }
}
