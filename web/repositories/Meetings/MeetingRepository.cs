using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Models;
using web.Repositories.Meetings.Dtos;
using web.Repositories.Meetings.Interfaces;
using web.Repositories.Meetings.Mappers;

namespace web.Repositories.Meetings;

public class MeetingRepository(WebDbContext dbContext) : IMeetingRepository
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 500;

    public async Task<MeetingPageDto> SearchAsync(SearchMeetingsDto search, CancellationToken cancellationToken = default)
    {
        var pageNumber = search.Page < 1 ? 1 : search.Page;
        var pageSize = search.PageSize is < DefaultPageSize or > MaxPageSize ? DefaultPageSize : search.PageSize;

        var meetingsQuery = dbContext.Meetings.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var text = search.SearchText.Trim().ToLower();
            meetingsQuery = meetingsQuery.Where(m => m.Title.ToLower().Contains(text) || (m.Location != null && m.Location.ToLower().Contains(text)));
        }
        if (search.Status.HasValue)
        {
            var status = search.Status.Value;
            meetingsQuery = meetingsQuery.Where(m => m.Status == status);
        }
        if (search.GroupIds.Count > 0)
        {
            var groupIds = search.GroupIds;
            meetingsQuery = meetingsQuery.Where(m => m.Groups.Any(g => groupIds.Contains(g.PersonGroupId)));
        }

        // Only the latest version of each meeting series is shown in the table.
        var latestPerSeries = dbContext.Meetings
            .GroupBy(m => m.RootMeetingId ?? m.Id)
            .Select(g => new { RootId = g.Key, MaxVersion = g.Max(m => m.VersionNumber), Count = g.Count() });

        var query =
            from m in meetingsQuery
            join lv in latestPerSeries
                on new { RootId = m.RootMeetingId ?? m.Id, Version = m.VersionNumber }
                equals new { RootId = lv.RootId, Version = lv.MaxVersion }
            select new { Meeting = m, lv.Count };

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(x => x.Meeting.MeetingDateUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                Item = new MeetingListItemDto
                {
                    Id = x.Meeting.Id,
                    Title = x.Meeting.Title,
                    MeetingDateUtc = x.Meeting.MeetingDateUtc,
                    Location = x.Meeting.Location,
                    Status = x.Meeting.Status,
                    VersionNumber = x.Meeting.VersionNumber,
                    VersionCount = x.Count,
                    Groups = x.Meeting.Groups
                        .OrderBy(g => g.PersonGroup.Name)
                        .Select(g => new MeetingGroupDto { GroupId = g.PersonGroupId, GroupName = g.PersonGroup.Name })
                        .ToList(),
                    AttendeeCount = x.Meeting.Attendees.Count,
                    AttendedCount = x.Meeting.Attendees.Count(a => a.HasAttended)
                },
                RootId = x.Meeting.RootMeetingId ?? x.Meeting.Id
            })
            .ToListAsync(cancellationToken);

        var seriesNeedingVersions = rows.Where(r => r.Item.VersionCount > 1).Select(r => r.RootId).Distinct().ToList();
        if (seriesNeedingVersions.Count > 0)
        {
            var allVersions = await dbContext.Meetings.AsNoTracking()
                .Where(m => seriesNeedingVersions.Contains(m.RootMeetingId ?? m.Id))
                .OrderByDescending(m => m.VersionNumber)
                .Select(m => new { m.Id, RootId = m.RootMeetingId ?? m.Id, m.VersionNumber, m.MeetingDateUtc, m.Status })
                .ToListAsync(cancellationToken);

            foreach (var row in rows.Where(r => r.Item.VersionCount > 1))
            {
                row.Item.Versions = allVersions
                    .Where(v => v.RootId == row.RootId)
                    .Select(v => new MeetingVersionOptionDto
                    {
                        Id = v.Id,
                        VersionNumber = v.VersionNumber,
                        MeetingDateUtc = v.MeetingDateUtc,
                        Status = v.Status,
                        IsCurrent = v.Id == row.Item.Id
                    })
                    .ToList();
            }
        }

        return new MeetingPageDto { Items = rows.Select(r => r.Item).ToList(), TotalCount = totalCount, Page = pageNumber, PageSize = pageSize };
    }

    public async Task<MeetingDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var meeting = await dbContext.Meetings.AsNoTracking()
            .Include(m => m.Groups).ThenInclude(g => g.PersonGroup)
            .Include(m => m.Attendees).ThenInclude(a => a.ApplicationUser)
            .Include(m => m.Decisions).ThenInclude(d => d.ResponsibleUser)
            .Include(m => m.Attachments).ThenInclude(a => a.FileMetadata)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (meeting is null)
            return null;

        var seriesVersions = await dbContext.Meetings.AsNoTracking()
            .Where(m => (m.RootMeetingId ?? m.Id) == (meeting.RootMeetingId ?? meeting.Id))
            .OrderByDescending(m => m.VersionNumber)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var isLatest = seriesVersions[0] == meeting.Id;
        var index = seriesVersions.IndexOf(meeting.Id);

        var dto = meeting.ToDto();
        dto.VersionInfo = new MeetingVersionInfoDto
        {
            VersionNumber = meeting.VersionNumber,
            VersionCount = seriesVersions.Count,
            IsLatestVersion = isLatest,
            CurrentVersionId = isLatest ? null : seriesVersions[0],
            PreviousVersionId = index + 1 < seriesVersions.Count ? seriesVersions[index + 1] : null,
            NextVersionId = index > 0 ? seriesVersions[index - 1] : null
        };
        return dto;
    }

    public async Task<bool> IsLatestVersionAsync(int id, CancellationToken cancellationToken = default)
    {
        var meeting = await dbContext.Meetings.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new { m.Id, m.RootMeetingId, m.VersionNumber })
            .FirstOrDefaultAsync(cancellationToken);
        if (meeting is null)
            return false;

        return meeting.VersionNumber == await LatestVersionNumberAsync(meeting.RootMeetingId ?? meeting.Id, cancellationToken);
    }

    public async Task<int> CreateAsync(CreateMeetingDto dto, CancellationToken cancellationToken = default)
    {
        var meeting = dto.ToEntity();
        dbContext.Meetings.Add(meeting);
        await dbContext.SaveChangesAsync(cancellationToken);
        return meeting.Id;
    }

    public async Task<SaveMeetingStatus> UpdateAsync(int id, UpdateMeetingDto dto, CancellationToken cancellationToken = default)
    {
        var meeting = await dbContext.Meetings
            .Include(m => m.Attendees)
            .Include(m => m.Groups)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (meeting is null)
            return SaveMeetingStatus.NotFound;
        if (meeting.VersionNumber != await LatestVersionNumberAsync(meeting.RootMeetingId ?? meeting.Id, cancellationToken))
            return SaveMeetingStatus.NotLatestVersion;

        // Groups and attendees are replaced; attendance is kept for attendees who stay.
        var previousAttendance = meeting.Attendees.ToDictionary(a => a.ApplicationUserId, a => a.HasAttended);
        dbContext.MeetingGroups.RemoveRange(meeting.Groups);
        meeting.Groups.Clear();
        dbContext.MeetingAttendees.RemoveRange(meeting.Attendees);
        meeting.Attendees.Clear();

        dto.ApplyTo(meeting, previousAttendance);
        dbContext.Entry(meeting).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return SaveMeetingStatus.Saved;
    }

    public async Task<CreateMeetingVersionResultDto> CreateNewVersionAsync(int sourceMeetingId, DateTime meetingDateUtc, CancellationToken cancellationToken = default)
    {
        var source = await dbContext.Meetings
            .Include(m => m.Attendees)
            .Include(m => m.Groups)
            .FirstOrDefaultAsync(m => m.Id == sourceMeetingId, cancellationToken);
        if (source is null)
            return new CreateMeetingVersionResultDto { Status = CreateMeetingVersionStatus.NotFound };
        if (source.Status == MeetingStatus.Planned)
            return new CreateMeetingVersionResultDto { Status = CreateMeetingVersionStatus.StillPlanned };

        var rootId = source.RootMeetingId ?? source.Id;
        var latestVersionNumber = await LatestVersionNumberAsync(rootId, cancellationToken);
        if (source.VersionNumber != latestVersionNumber)
            return new CreateMeetingVersionResultDto { Status = CreateMeetingVersionStatus.NotLatestVersion };

        var newVersion = new Meeting
        {
            Title = source.Title,
            MeetingDateUtc = meetingDateUtc,
            Location = source.Location,
            Status = MeetingStatus.Planned,
            AgendaNotes = source.AgendaNotes,
            RootMeetingId = rootId,
            VersionNumber = latestVersionNumber + 1
        };
        foreach (var attendee in source.Attendees)
            newVersion.Attendees.Add(new MeetingAttendee { ApplicationUserId = attendee.ApplicationUserId, HasAttended = false });
        foreach (var group in source.Groups)
            newVersion.Groups.Add(new MeetingGroup { PersonGroupId = group.PersonGroupId });

        dbContext.Meetings.Add(newVersion);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CreateMeetingVersionResultDto { Status = CreateMeetingVersionStatus.Created, MeetingId = newVersion.Id };
    }

    public async Task<List<int>?> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var meeting = await dbContext.Meetings
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (meeting is null)
            return null;

        // Deleting the root of a version series: promote the oldest remaining sibling to be the
        // new root first, so the series stays linked.
        if (meeting.RootMeetingId is null)
        {
            var siblings = await dbContext.Meetings
                .Where(m => m.RootMeetingId == meeting.Id)
                .OrderBy(m => m.VersionNumber)
                .ToListAsync(cancellationToken);

            if (siblings.Count > 0)
            {
                var newRoot = siblings[0];
                newRoot.RootMeetingId = null;
                foreach (var sibling in siblings.Skip(1))
                    sibling.RootMeetingId = newRoot.Id;
            }
        }

        var fileIds = meeting.Attachments.Select(a => a.FileMetadataId).ToList();
        dbContext.Meetings.Remove(meeting);
        await dbContext.SaveChangesAsync(cancellationToken);
        return fileIds;
    }

    public async Task<bool> SetAttendanceAsync(int meetingId, string userId, bool hasAttended, CancellationToken cancellationToken = default)
    {
        if (!await IsLatestVersionAsync(meetingId, cancellationToken))
            return false;

        var attendee = await dbContext.MeetingAttendees
            .FirstOrDefaultAsync(a => a.MeetingId == meetingId && a.ApplicationUserId == userId, cancellationToken);
        if (attendee is null)
            return false;

        attendee.HasAttended = hasAttended;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SaveNotesAsync(int meetingId, string? agendaNotes, string? minutesNotes, CancellationToken cancellationToken = default)
    {
        if (!await IsLatestVersionAsync(meetingId, cancellationToken))
            return false;

        var meeting = await dbContext.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);
        if (meeting is null)
            return false;

        meeting.AgendaNotes = agendaNotes;
        meeting.MinutesNotes = minutesNotes;
        dbContext.Entry(meeting).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MeetingDecisionDto?> AddDecisionAsync(int meetingId, CreateMeetingDecisionDto dto, CancellationToken cancellationToken = default)
    {
        if (!await IsLatestVersionAsync(meetingId, cancellationToken))
            return null;

        var decision = dto.ToEntity(meetingId);
        dbContext.MeetingDecisions.Add(decision);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.MeetingDecisions.AsNoTracking()
            .Where(d => d.Id == decision.Id)
            .Select(MeetingMapper.ToDecisionDtoExpression)
            .FirstAsync(cancellationToken);
    }

    public async Task<bool> UpdateDecisionAsync(int decisionId, UpdateMeetingDecisionDto dto, CancellationToken cancellationToken = default)
    {
        var decision = await FindEditableDecisionAsync(decisionId, cancellationToken);
        if (decision is null)
            return false;

        dto.ApplyTo(decision);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetDecisionCompletionAsync(int decisionId, bool isCompleted, CancellationToken cancellationToken = default)
    {
        var decision = await FindEditableDecisionAsync(decisionId, cancellationToken);
        if (decision is null)
            return false;

        decision.IsCompleted = isCompleted;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteDecisionAsync(int decisionId, CancellationToken cancellationToken = default)
    {
        var decision = await FindEditableDecisionAsync(decisionId, cancellationToken);
        if (decision is null)
            return false;

        dbContext.MeetingDecisions.Remove(decision);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MeetingAttachmentDto?> AddAttachmentAsync(int meetingId, int fileId, bool isRecording, CancellationToken cancellationToken = default)
    {
        if (!await IsLatestVersionAsync(meetingId, cancellationToken))
            return null;

        var attachment = new MeetingAttachment { MeetingId = meetingId, FileMetadataId = fileId, IsRecording = isRecording };
        dbContext.MeetingAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(attachment).Reference(a => a.FileMetadata).LoadAsync(cancellationToken);
        return attachment.ToDto();
    }

    public async Task<int?> RemoveAttachmentAsync(int attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await dbContext.MeetingAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);
        if (attachment is null || !await IsLatestVersionAsync(attachment.MeetingId, cancellationToken))
            return null;

        dbContext.MeetingAttachments.Remove(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return attachment.FileMetadataId;
    }

    public Task<int?> GetAttachmentFileIdAsync(int attachmentId, CancellationToken cancellationToken = default) =>
        dbContext.MeetingAttachments.AsNoTracking()
            .Where(a => a.Id == attachmentId)
            .Select(a => (int?)a.FileMetadataId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<RequestTranscriptionStatus> RequestTranscriptionAsync(int attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await dbContext.MeetingAttachments
            .Include(a => a.FileMetadata)
            .FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);
        if (attachment is null)
            return RequestTranscriptionStatus.NotFound;
        if (!await IsLatestVersionAsync(attachment.MeetingId, cancellationToken))
            return RequestTranscriptionStatus.NotLatestVersion;
        if (!attachment.FileMetadata.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            return RequestTranscriptionStatus.NotAudio;
        if (TranscriptionStatuses.IsInProgress(attachment.TranscriptionStatus))
            return RequestTranscriptionStatus.AlreadyInProgress;

        attachment.TranscriptionStatus = TranscriptionStatus.Queued;
        attachment.TranscriptionError = null;
        attachment.TranscriptionStartedAtUtc = null;
        attachment.TranscriptionCompletedAtUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return RequestTranscriptionStatus.Queued;
    }

    public Task<TranscriptionStatusDto?> GetTranscriptionStatusAsync(int attachmentId, CancellationToken cancellationToken = default) =>
        dbContext.MeetingAttachments.AsNoTracking()
            .Where(a => a.Id == attachmentId)
            .Select(a => new TranscriptionStatusDto { AttachmentId = a.Id, Status = a.TranscriptionStatus, Error = a.TranscriptionError })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<TranscriptionJobDto?> StartTranscriptionAsync(int attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await dbContext.MeetingAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);
        if (attachment is null)
            return null;

        attachment.TranscriptionStatus = TranscriptionStatus.Processing;
        attachment.TranscriptionStartedAtUtc = DateTime.UtcNow;
        attachment.TranscriptionError = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new TranscriptionJobDto { AttachmentId = attachment.Id, MeetingId = attachment.MeetingId, FileId = attachment.FileMetadataId };
    }

    public async Task CompleteTranscriptionAsync(int attachmentId, string text, CancellationToken cancellationToken = default)
    {
        var attachment = await dbContext.MeetingAttachments
            .Include(a => a.Meeting)
            .FirstAsync(a => a.Id == attachmentId, cancellationToken);

        var meeting = attachment.Meeting;
        meeting.MinutesNotes = string.IsNullOrWhiteSpace(meeting.MinutesNotes) ? text : $"{meeting.MinutesNotes}\n\n{text}";
        dbContext.Entry(meeting).State = EntityState.Modified;

        attachment.TranscriptionStatus = TranscriptionStatus.Completed;
        attachment.TranscriptionCompletedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task FailTranscriptionAsync(int attachmentId, string error, CancellationToken cancellationToken = default)
    {
        var attachment = await dbContext.MeetingAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);
        if (attachment is null)
            return;

        attachment.TranscriptionStatus = TranscriptionStatus.Failed;
        attachment.TranscriptionError = error;
        attachment.TranscriptionCompletedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The decision, unless it doesn't exist or its meeting is an older version (read-only).</summary>
    private async Task<MeetingDecision?> FindEditableDecisionAsync(int decisionId, CancellationToken cancellationToken)
    {
        var decision = await dbContext.MeetingDecisions.FirstOrDefaultAsync(d => d.Id == decisionId, cancellationToken);
        return decision is not null && await IsLatestVersionAsync(decision.MeetingId, cancellationToken) ? decision : null;
    }

    private Task<int> LatestVersionNumberAsync(int rootId, CancellationToken cancellationToken) =>
        dbContext.Meetings
            .Where(m => (m.RootMeetingId ?? m.Id) == rootId)
            .MaxAsync(m => m.VersionNumber, cancellationToken);
}
