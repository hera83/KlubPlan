using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web.BgSerives;
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
        private readonly ITranscriptionQueue _transcriptionQueue;
        private readonly ILogger<MeetingsService> _logger;

        public MeetingsService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            IConfiguration config,
            ITranscriptionQueue transcriptionQueue,
            ILogger<MeetingsService> logger)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _config = config;
            _transcriptionQueue = transcriptionQueue;
            _logger = logger;
        }

        public async Task<MeetingFilterViewModel> GetMeetingsAsync(MeetingFilterViewModel filter, CancellationToken ct = default)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize is < 10 or > 500 ? 10 : filter.PageSize;

            var meetingsQuery = _context.Meetings
                .Include(m => m.Groups).ThenInclude(g => g.PersonGroup)
                .Include(m => m.Attendees)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var search = filter.SearchText.Trim().ToLower();
                meetingsQuery = meetingsQuery.Where(m =>
                    m.Title.ToLower().Contains(search) ||
                    (m.Location != null && m.Location.ToLower().Contains(search)));
            }

            if (filter.Status.HasValue)
            {
                meetingsQuery = meetingsQuery.Where(m => m.Status == filter.Status.Value);
            }

            if (filter.GroupIds.Count > 0)
            {
                meetingsQuery = meetingsQuery.Where(m => m.Groups.Any(g => filter.GroupIds.Contains(g.PersonGroupId)));
            }

            // Only the latest version of each meeting series is shown in the table.
            var latestPerSeries = _context.Meetings
                .GroupBy(m => m.RootMeetingId ?? m.Id)
                .Select(g => new { RootId = g.Key, MaxVersion = g.Max(m => m.VersionNumber), Count = g.Count() });

            var query =
                from m in meetingsQuery
                join lv in latestPerSeries
                    on new { RootId = m.RootMeetingId ?? m.Id, Version = m.VersionNumber }
                    equals new { RootId = lv.RootId, Version = lv.MaxVersion }
                select new { Meeting = m, lv.Count };

            filter.TotalCount = await query.CountAsync(ct);

            var page = await query
                .OrderByDescending(x => x.Meeting.MeetingDateUtc)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new
                {
                    x.Meeting.Id,
                    x.Meeting.Title,
                    x.Meeting.MeetingDateUtc,
                    x.Meeting.Location,
                    x.Meeting.Status,
                    x.Meeting.VersionNumber,
                    x.Meeting.RootMeetingId,
                    VersionCount = x.Count,
                    Groups = x.Meeting.Groups
                        .OrderBy(g => g.PersonGroup.Name)
                        .Select(g => new MeetingGroupItemViewModel { GroupId = g.PersonGroupId, GroupName = g.PersonGroup.Name })
                        .ToList(),
                    AttendeeCount = x.Meeting.Attendees.Count,
                    AttendedCount = x.Meeting.Attendees.Count(a => a.HasAttended)
                })
                .ToListAsync(ct);

            filter.Meetings = page.Select(p => new MeetingListItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                MeetingDateUtc = p.MeetingDateUtc,
                Location = p.Location,
                Status = p.Status,
                Groups = p.Groups,
                AttendeeCount = p.AttendeeCount,
                AttendedCount = p.AttendedCount,
                VersionNumber = p.VersionNumber,
                VersionCount = p.VersionCount
            }).ToList();

            var seriesNeedingVersions = page
                .Where(p => p.VersionCount > 1)
                .Select(p => p.RootMeetingId ?? p.Id)
                .Distinct()
                .ToList();

            if (seriesNeedingVersions.Count > 0)
            {
                var allVersions = await _context.Meetings
                    .Where(m => seriesNeedingVersions.Contains(m.RootMeetingId ?? m.Id))
                    .OrderByDescending(m => m.VersionNumber)
                    .Select(m => new { m.Id, RootId = m.RootMeetingId ?? m.Id, m.VersionNumber, m.MeetingDateUtc, m.Status })
                    .ToListAsync(ct);

                var rootByMeetingId = page.ToDictionary(p => p.Id, p => p.RootMeetingId ?? p.Id);

                foreach (var item in filter.Meetings)
                {
                    if (item.VersionCount <= 1)
                        continue;

                    var rootId = rootByMeetingId[item.Id];
                    item.Versions = allVersions
                        .Where(v => v.RootId == rootId)
                        .Select(v => new MeetingVersionOptionViewModel
                        {
                            Id = v.Id,
                            VersionNumber = v.VersionNumber,
                            MeetingDateUtc = v.MeetingDateUtc,
                            Status = v.Status,
                            IsCurrent = v.Id == item.Id
                        })
                        .ToList();
                }
            }

            filter.Groups = await _context.PersonGroups
                .OrderBy(g => g.Name)
                .Select(g => new PersonGroupOptionViewModel { Id = g.Id, Name = g.Name })
                .ToListAsync(ct);

            return filter;
        }

        public async Task<MeetingDetailViewModel?> GetMeetingDetailAsync(int id, CancellationToken ct = default)
        {
            var meeting = await _context.Meetings
                .Include(m => m.Groups).ThenInclude(g => g.PersonGroup)
                .Include(m => m.Attendees).ThenInclude(a => a.ApplicationUser)
                .Include(m => m.Decisions).ThenInclude(d => d.ResponsibleUser)
                .Include(m => m.Attachments).ThenInclude(a => a.FileMetadata)
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (meeting is null)
                return null;

            var effectiveRootId = meeting.RootMeetingId ?? meeting.Id;
            var seriesVersions = await _context.Meetings
                .Where(m => (m.RootMeetingId ?? m.Id) == effectiveRootId)
                .OrderByDescending(m => m.VersionNumber)
                .Select(m => new { m.Id, m.VersionNumber })
                .ToListAsync(ct);

            var latestInSeries = seriesVersions[0];
            var isLatestVersion = latestInSeries.Id == meeting.Id;
            var currentIndex = seriesVersions.FindIndex(v => v.Id == meeting.Id);

            return new MeetingDetailViewModel
            {
                Id = meeting.Id,
                VersionNumber = meeting.VersionNumber,
                VersionCount = seriesVersions.Count,
                IsLatestVersion = isLatestVersion,
                CurrentVersionId = isLatestVersion ? null : latestInSeries.Id,
                PreviousVersionId = currentIndex + 1 < seriesVersions.Count ? seriesVersions[currentIndex + 1].Id : null,
                NextVersionId = currentIndex > 0 ? seriesVersions[currentIndex - 1].Id : null,
                Title = meeting.Title,
                MeetingDateUtc = meeting.MeetingDateUtc,
                Location = meeting.Location,
                Status = meeting.Status,
                Groups = meeting.Groups
                    .OrderBy(g => g.PersonGroup.Name)
                    .Select(g => new MeetingGroupItemViewModel { GroupId = g.PersonGroupId, GroupName = g.PersonGroup.Name })
                    .ToList(),
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
                        CreatedAtUtc = a.CreatedAtUtc,
                        TranscriptionStatus = a.TranscriptionStatus,
                        TranscriptionError = a.TranscriptionError
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
                AgendaNotes = dto.AgendaNotes,
                CreatedAtUtc = DateTime.UtcNow
            };

            foreach (var groupId in dto.GroupIds.Distinct())
            {
                meeting.Groups.Add(new MeetingGroup { PersonGroupId = groupId });
            }

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
                .Include(m => m.Groups)
                .FirstOrDefaultAsync(m => m.Id == dto.Id, ct);

            if (meeting is null)
                return new UpdateMeetingResponseDto { Success = false, ErrorMessage = "Mødet blev ikke fundet." };

            var effectiveRootId = meeting.RootMeetingId ?? meeting.Id;
            var latestVersionNumber = await _context.Meetings
                .Where(m => (m.RootMeetingId ?? m.Id) == effectiveRootId)
                .MaxAsync(m => m.VersionNumber, ct);

            if (meeting.VersionNumber != latestVersionNumber)
                return new UpdateMeetingResponseDto { Success = false, ErrorMessage = "Der findes en nyere version af dette møde — kun den aktuelle version kan redigeres." };

            meeting.Title = dto.Title.Trim();
            meeting.MeetingDateUtc = dto.MeetingDateUtc;
            meeting.Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
            meeting.AgendaNotes = dto.AgendaNotes;
            meeting.Status = dto.Status;
            meeting.UpdatedAtUtc = DateTime.UtcNow;

            _context.MeetingGroups.RemoveRange(meeting.Groups);
            meeting.Groups.Clear();
            foreach (var groupId in dto.GroupIds.Distinct())
            {
                meeting.Groups.Add(new MeetingGroup { PersonGroupId = groupId });
            }

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

        public async Task<CreateMeetingResponseDto> CreateNewVersionAsync(int sourceMeetingId, DateTime meetingDateUtc, CancellationToken ct = default)
        {
            var source = await _context.Meetings
                .Include(m => m.Attendees)
                .Include(m => m.Groups)
                .FirstOrDefaultAsync(m => m.Id == sourceMeetingId, ct);

            if (source is null)
                return new CreateMeetingResponseDto { Success = false, ErrorMessage = "Mødet blev ikke fundet." };

            if (source.Status == MeetingStatus.Planned)
                return new CreateMeetingResponseDto { Success = false, ErrorMessage = "Der kan kun oprettes en ny version, når mødet er afholdt eller aflyst." };

            var effectiveRootId = source.RootMeetingId ?? source.Id;
            var latestVersionNumber = await _context.Meetings
                .Where(m => (m.RootMeetingId ?? m.Id) == effectiveRootId)
                .MaxAsync(m => m.VersionNumber, ct);

            if (source.VersionNumber != latestVersionNumber)
                return new CreateMeetingResponseDto { Success = false, ErrorMessage = "Der findes allerede en nyere version af dette møde." };

            var newVersion = new Meeting
            {
                Title = source.Title,
                MeetingDateUtc = meetingDateUtc,
                Location = source.Location,
                Status = MeetingStatus.Planned,
                AgendaNotes = source.AgendaNotes,
                RootMeetingId = effectiveRootId,
                VersionNumber = latestVersionNumber + 1,
                CreatedAtUtc = DateTime.UtcNow
            };

            foreach (var attendee in source.Attendees)
            {
                newVersion.Attendees.Add(new MeetingAttendee { ApplicationUserId = attendee.ApplicationUserId, HasAttended = false });
            }

            foreach (var group in source.Groups)
            {
                newVersion.Groups.Add(new MeetingGroup { PersonGroupId = group.PersonGroupId });
            }

            _context.Meetings.Add(newVersion);
            await _context.SaveChangesAsync(ct);

            return new CreateMeetingResponseDto { Success = true, MeetingId = newVersion.Id };
        }

        public async Task<bool> DeleteMeetingAsync(int id, CancellationToken ct = default)
        {
            var meeting = await _context.Meetings
                .Include(m => m.Attachments).ThenInclude(a => a.FileMetadata)
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (meeting is null)
                return false;

            // If we're deleting the root of a version series, promote the oldest remaining
            // sibling to be the new root before removing this one, so the series stays linked.
            if (meeting.RootMeetingId is null)
            {
                var siblings = await _context.Meetings
                    .Where(m => m.RootMeetingId == meeting.Id)
                    .OrderBy(m => m.VersionNumber)
                    .ToListAsync(ct);

                if (siblings.Count > 0)
                {
                    var newRoot = siblings[0];
                    newRoot.RootMeetingId = null;
                    foreach (var sibling in siblings.Skip(1))
                    {
                        sibling.RootMeetingId = newRoot.Id;
                    }
                }
            }

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
            if (!await IsLatestVersionAsync(meetingId, ct))
                return false;

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
            if (!await IsLatestVersionAsync(meetingId, ct))
                return false;

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
            if (!await IsLatestVersionAsync(meetingId, ct))
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

            if (!await IsLatestVersionAsync(decision.MeetingId, ct))
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

            if (!await IsLatestVersionAsync(decision.MeetingId, ct))
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

            if (!await IsLatestVersionAsync(decision.MeetingId, ct))
                return false;

            _context.MeetingDecisions.Remove(decision);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<MeetingAttachmentViewModel?> AddAttachmentAsync(int meetingId, Stream fileStream, string originalFileName, string contentType, string? uploaderId, bool isRecording, CancellationToken ct = default)
        {
            if (!await IsLatestVersionAsync(meetingId, ct))
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

            if (!await IsLatestVersionAsync(attachment.MeetingId, ct))
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

        public async Task<(bool Success, string? ErrorMessage)> RequestTranscriptionAsync(int attachmentId, CancellationToken ct = default)
        {
            var attachment = await _context.MeetingAttachments
                .Include(a => a.FileMetadata)
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

            if (attachment is null)
                return (false, "Filen blev ikke fundet.");

            if (!await IsLatestVersionAsync(attachment.MeetingId, ct))
                return (false, "Der findes en nyere version af dette møde — denne version er skrivebeskyttet.");

            if (!attachment.FileMetadata.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
                return (false, "Kun lydfiler kan transskriberes.");

            if (TranscriptionStatuses.IsInProgress(attachment.TranscriptionStatus))
                return (false, "Transskription er allerede i gang for denne fil.");

            attachment.TranscriptionStatus = TranscriptionStatus.Queued;
            attachment.TranscriptionError = null;
            attachment.TranscriptionStartedAtUtc = null;
            attachment.TranscriptionCompletedAtUtc = null;
            await _context.SaveChangesAsync(ct);

            _transcriptionQueue.Enqueue(attachmentId);
            return (true, null);
        }

        public async Task<TranscriptionStatusViewModel?> GetTranscriptionStatusAsync(int attachmentId, CancellationToken ct = default)
        {
            var attachment = await _context.MeetingAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

            if (attachment is null)
                return null;

            return new TranscriptionStatusViewModel
            {
                AttachmentId = attachment.Id,
                Status = attachment.TranscriptionStatus,
                Error = attachment.TranscriptionError
            };
        }

        private void DeletePhysicalFile(string storedRelativePath)
        {
            var fullPath = Path.Combine(_env.ContentRootPath, storedRelativePath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        /// <summary>
        /// Older versions of a meeting are read-only once a newer version exists — this guards
        /// every write path (notes, attendance, decisions, attachments) against editing them,
        /// mirroring the check already applied in UpdateMeetingAsync.
        /// </summary>
        private async Task<bool> IsLatestVersionAsync(int meetingId, CancellationToken ct)
        {
            var meeting = await _context.Meetings
                .AsNoTracking()
                .Select(m => new { m.Id, m.RootMeetingId, m.VersionNumber })
                .FirstOrDefaultAsync(m => m.Id == meetingId, ct);

            if (meeting is null)
                return false;

            var effectiveRootId = meeting.RootMeetingId ?? meeting.Id;
            var latestVersionNumber = await _context.Meetings
                .Where(m => (m.RootMeetingId ?? m.Id) == effectiveRootId)
                .MaxAsync(m => m.VersionNumber, ct);

            return meeting.VersionNumber == latestVersionNumber;
        }
    }
}
