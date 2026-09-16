using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Repositories.Meetings.Dtos;
using web.Repositories.Meetings.Interfaces;
using web.ViewModels;

namespace web.Repositories.Meetings
{
    public class MeetingsService : IMeetingsService
    {
        private static readonly string[] AdminRoles = { AppRoles.Administrator, AppRoles.Developer };

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<MeetingsService> _logger;

        public MeetingsService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<MeetingsService> logger)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _config = config;
            _logger = logger;
        }

        public async Task<MeetingFilterViewModel> GetMeetingsAsync(MeetingFilterViewModel filter, CancellationToken ct = default)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize is < 10 or > 500 ? 10 : filter.PageSize;

            var query = _context.Meetings
                .Include(m => m.PersonGroup)
                .Include(m => m.Attendees)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var search = filter.SearchText.Trim().ToLower();
                query = query.Where(m =>
                    m.Title.ToLower().Contains(search) ||
                    (m.Location != null && m.Location.ToLower().Contains(search)));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(m => m.Status == filter.Status.Value);
            }

            if (filter.PersonGroupId.HasValue)
            {
                query = query.Where(m => m.PersonGroupId == filter.PersonGroupId.Value);
            }

            filter.TotalCount = await query.CountAsync(ct);

            filter.Meetings = await query
                .OrderByDescending(m => m.MeetingDateUtc)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(m => new MeetingListItemViewModel
                {
                    Id = m.Id,
                    Title = m.Title,
                    MeetingDateUtc = m.MeetingDateUtc,
                    Location = m.Location,
                    Status = m.Status,
                    PersonGroupName = m.PersonGroup != null ? m.PersonGroup.Name : null,
                    AttendeeCount = m.Attendees.Count,
                    AttendedCount = m.Attendees.Count(a => a.HasAttended)
                })
                .ToListAsync(ct);

            filter.Groups = await _context.PersonGroups
                .OrderBy(g => g.Name)
                .Select(g => new PersonGroupOptionViewModel { Id = g.Id, Name = g.Name })
                .ToListAsync(ct);

            return filter;
        }

        public async Task<MeetingDetailViewModel?> GetMeetingDetailAsync(int id, CancellationToken ct = default)
        {
            var meeting = await _context.Meetings
                .Include(m => m.PersonGroup)
                .Include(m => m.Attendees).ThenInclude(a => a.ApplicationUser)
                .Include(m => m.Decisions).ThenInclude(d => d.ResponsibleUser)
                .Include(m => m.Attachments).ThenInclude(a => a.FileMetadata)
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (meeting is null)
                return null;

            return new MeetingDetailViewModel
            {
                Id = meeting.Id,
                Title = meeting.Title,
                MeetingDateUtc = meeting.MeetingDateUtc,
                Location = meeting.Location,
                Status = meeting.Status,
                PersonGroupId = meeting.PersonGroupId,
                PersonGroupName = meeting.PersonGroup?.Name,
                AgendaNotes = meeting.AgendaNotes,
                MinutesNotes = meeting.MinutesNotes,
                Attendees = meeting.Attendees
                    .OrderBy(a => a.ApplicationUser.DisplayName)
                    .Select(a => new MeetingAttendeeViewModel
                    {
                        UserId = a.ApplicationUserId,
                        DisplayName = a.ApplicationUser.DisplayName,
                        HasAttended = a.HasAttended
                    })
                    .ToList(),
                Decisions = meeting.Decisions
                    .OrderBy(d => d.CreatedAtUtc)
                    .Select(d => new MeetingDecisionViewModel
                    {
                        Id = d.Id,
                        Description = d.Description,
                        ResponsibleUserId = d.ResponsibleUserId,
                        ResponsibleUserName = d.ResponsibleUser?.DisplayName,
                        DueDate = d.DueDate,
                        IsCompleted = d.IsCompleted
                    })
                    .ToList(),
                Attachments = meeting.Attachments
                    .OrderByDescending(a => a.CreatedAtUtc)
                    .Select(a => new MeetingAttachmentViewModel
                    {
                        Id = a.Id,
                        OriginalFileName = a.FileMetadata.OriginalFileName,
                        ContentType = a.FileMetadata.ContentType,
                        FileSizeBytes = a.FileMetadata.FileSizeBytes,
                        IsRecording = a.IsRecording,
                        CreatedAtUtc = a.CreatedAtUtc
                    })
                    .ToList()
            };
        }

        public async Task<List<MeetingAdminOptionViewModel>> GetAdminOptionsAsync(CancellationToken ct = default)
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

        public async Task<List<PersonGroupOptionViewModel>> GetGroupOptionsAsync(CancellationToken ct = default)
        {
            return await _context.PersonGroups
                .OrderBy(g => g.Name)
                .Select(g => new PersonGroupOptionViewModel { Id = g.Id, Name = g.Name })
                .ToListAsync(ct);
        }

        public async Task<CreateMeetingResponseDto> CreateMeetingAsync(CreateMeetingRequestDto dto, CancellationToken ct = default)
        {
            var meeting = new Meeting
            {
                Title = dto.Title.Trim(),
                MeetingDateUtc = dto.MeetingDateUtc,
                Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim(),
                PersonGroupId = dto.PersonGroupId,
                AgendaNotes = dto.AgendaNotes,
                CreatedAtUtc = DateTime.UtcNow
            };

            foreach (var userId in dto.AttendeeUserIds.Distinct())
            {
                meeting.Attendees.Add(new MeetingAttendee { ApplicationUserId = userId, HasAttended = false });
            }

            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync(ct);

            return new CreateMeetingResponseDto { Success = true, MeetingId = meeting.Id };
        }

        public async Task<UpdateMeetingResponseDto> UpdateMeetingAsync(UpdateMeetingRequestDto dto, CancellationToken ct = default)
        {
            var meeting = await _context.Meetings
                .Include(m => m.Attendees)
                .FirstOrDefaultAsync(m => m.Id == dto.Id, ct);

            if (meeting is null)
                return new UpdateMeetingResponseDto { Success = false, ErrorMessage = "Mødet blev ikke fundet." };

            meeting.Title = dto.Title.Trim();
            meeting.MeetingDateUtc = dto.MeetingDateUtc;
            meeting.Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
            meeting.PersonGroupId = dto.PersonGroupId;
            meeting.AgendaNotes = dto.AgendaNotes;
            meeting.Status = dto.Status;
            meeting.UpdatedAtUtc = DateTime.UtcNow;

            // Preserve HasAttended for attendees kept across the edit; drop and (re-)add the rest.
            var previousAttendance = meeting.Attendees.ToDictionary(a => a.ApplicationUserId, a => a.HasAttended);
            _context.MeetingAttendees.RemoveRange(meeting.Attendees);
            meeting.Attendees.Clear();

            foreach (var userId in dto.AttendeeUserIds.Distinct())
            {
                meeting.Attendees.Add(new MeetingAttendee
                {
                    ApplicationUserId = userId,
                    HasAttended = previousAttendance.TryGetValue(userId, out var hasAttended) && hasAttended
                });
            }

            await _context.SaveChangesAsync(ct);
            return new UpdateMeetingResponseDto { Success = true };
        }

        public async Task<bool> DeleteMeetingAsync(int id, CancellationToken ct = default)
        {
            var meeting = await _context.Meetings
                .Include(m => m.Attachments).ThenInclude(a => a.FileMetadata)
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (meeting is null)
                return false;

            foreach (var attachment in meeting.Attachments)
            {
                DeletePhysicalFile(attachment.FileMetadata.StoredPath);
            }

            _context.Meetings.Remove(meeting);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> SetAttendanceAsync(int meetingId, string userId, bool hasAttended, CancellationToken ct = default)
        {
            var attendee = await _context.MeetingAttendees
                .FirstOrDefaultAsync(a => a.MeetingId == meetingId && a.ApplicationUserId == userId, ct);

            if (attendee is null)
                return false;

            attendee.HasAttended = hasAttended;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> SaveNotesAsync(int meetingId, string? agendaNotes, string? minutesNotes, CancellationToken ct = default)
        {
            var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId, ct);
            if (meeting is null)
                return false;

            meeting.AgendaNotes = agendaNotes;
            meeting.MinutesNotes = minutesNotes;
            meeting.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<MeetingDecisionViewModel?> AddDecisionAsync(int meetingId, string description, string? responsibleUserId, DateOnly? dueDate, CancellationToken ct = default)
        {
            var meetingExists = await _context.Meetings.AnyAsync(m => m.Id == meetingId, ct);
            if (!meetingExists)
                return null;

            var decision = new MeetingDecision
            {
                MeetingId = meetingId,
                Description = description.Trim(),
                ResponsibleUserId = string.IsNullOrWhiteSpace(responsibleUserId) ? null : responsibleUserId,
                DueDate = dueDate,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.MeetingDecisions.Add(decision);
            await _context.SaveChangesAsync(ct);

            string? responsibleUserName = null;
            if (decision.ResponsibleUserId is not null)
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == decision.ResponsibleUserId, ct);
                responsibleUserName = user?.DisplayName;
            }

            return new MeetingDecisionViewModel
            {
                Id = decision.Id,
                Description = decision.Description,
                ResponsibleUserId = decision.ResponsibleUserId,
                ResponsibleUserName = responsibleUserName,
                DueDate = decision.DueDate,
                IsCompleted = decision.IsCompleted
            };
        }

        public async Task<bool> UpdateDecisionAsync(int decisionId, string description, string? responsibleUserId, DateOnly? dueDate, bool isCompleted, CancellationToken ct = default)
        {
            var decision = await _context.MeetingDecisions.FirstOrDefaultAsync(d => d.Id == decisionId, ct);
            if (decision is null)
                return false;

            decision.Description = description.Trim();
            decision.ResponsibleUserId = string.IsNullOrWhiteSpace(responsibleUserId) ? null : responsibleUserId;
            decision.DueDate = dueDate;
            decision.IsCompleted = isCompleted;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> SetDecisionCompletionAsync(int decisionId, bool isCompleted, CancellationToken ct = default)
        {
            var decision = await _context.MeetingDecisions.FirstOrDefaultAsync(d => d.Id == decisionId, ct);
            if (decision is null)
                return false;

            decision.IsCompleted = isCompleted;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteDecisionAsync(int decisionId, CancellationToken ct = default)
        {
            var decision = await _context.MeetingDecisions.FirstOrDefaultAsync(d => d.Id == decisionId, ct);
            if (decision is null)
                return false;

            _context.MeetingDecisions.Remove(decision);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<MeetingAttachmentViewModel?> AddAttachmentAsync(int meetingId, Stream fileStream, string originalFileName, string contentType, string? uploaderId, bool isRecording, CancellationToken ct = default)
        {
            var meetingExists = await _context.Meetings.AnyAsync(m => m.Id == meetingId, ct);
            if (!meetingExists)
                return null;

            var filesPath = _config["AppSettings:FilesPath"] ?? "App_files";
            var meetingsDir = Path.Combine(_env.ContentRootPath, filesPath, FileCategories.Meetings);
            Directory.CreateDirectory(meetingsDir);

            var ext = Path.GetExtension(originalFileName);
            var storedFileName = $"{Guid.NewGuid()}{ext}";
            var storedRelativePath = Path.Combine(filesPath, FileCategories.Meetings, storedFileName);
            var fullPath = Path.Combine(_env.ContentRootPath, storedRelativePath);

            await using (var target = File.Create(fullPath))
            {
                await fileStream.CopyToAsync(target, ct);
            }

            var fileInfo = new FileInfo(fullPath);

            var metadata = new FileMetadata
            {
                OriginalFileName = originalFileName,
                StoredFileName = storedFileName,
                StoredPath = storedRelativePath,
                ContentType = contentType,
                FileSizeBytes = fileInfo.Length,
                OwnerId = uploaderId,
                Category = FileCategories.Meetings,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.FileMetadata.Add(metadata);
            await _context.SaveChangesAsync(ct);

            var attachment = new MeetingAttachment
            {
                MeetingId = meetingId,
                FileMetadataId = metadata.Id,
                IsRecording = isRecording,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.MeetingAttachments.Add(attachment);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Meeting {MeetingId} got attachment {FileName} (recording: {IsRecording})", meetingId, storedFileName, isRecording);

            return new MeetingAttachmentViewModel
            {
                Id = attachment.Id,
                OriginalFileName = metadata.OriginalFileName,
                ContentType = metadata.ContentType,
                FileSizeBytes = metadata.FileSizeBytes,
                IsRecording = attachment.IsRecording,
                CreatedAtUtc = attachment.CreatedAtUtc
            };
        }

        public async Task<bool> DeleteAttachmentAsync(int attachmentId, CancellationToken ct = default)
        {
            var attachment = await _context.MeetingAttachments
                .Include(a => a.FileMetadata)
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

            if (attachment is null)
                return false;

            DeletePhysicalFile(attachment.FileMetadata.StoredPath);

            var metadata = attachment.FileMetadata;
            metadata.IsDeleted = true;
            metadata.DeletedAtUtc = DateTime.UtcNow;
            metadata.UpdatedAtUtc = DateTime.UtcNow;

            _context.MeetingAttachments.Remove(attachment);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<(byte[] Data, string ContentType, string FileName)?> GetAttachmentFileAsync(int attachmentId, CancellationToken ct = default)
        {
            var attachment = await _context.MeetingAttachments
                .Include(a => a.FileMetadata)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

            if (attachment is null || attachment.FileMetadata.IsDeleted)
                return null;

            var fullPath = Path.Combine(_env.ContentRootPath, attachment.FileMetadata.StoredPath);
            if (!File.Exists(fullPath))
                return null;

            var data = await File.ReadAllBytesAsync(fullPath, ct);
            return (data, attachment.FileMetadata.ContentType, attachment.FileMetadata.OriginalFileName);
        }

        private void DeletePhysicalFile(string storedRelativePath)
        {
            var fullPath = Path.Combine(_env.ContentRootPath, storedRelativePath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
