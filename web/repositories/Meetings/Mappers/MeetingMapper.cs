using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.Meetings.Dtos;

namespace web.Repositories.Meetings.Mappers;

public static class MeetingMapper
{
    public static readonly Expression<Func<MeetingDecision, MeetingDecisionDto>> ToDecisionDtoExpression = x => new MeetingDecisionDto
    {
        Id = x.Id,
        Description = x.Description,
        ResponsibleUserId = x.ResponsibleUserId,
        ResponsibleUserName = x.ResponsibleUser != null ? x.ResponsibleUser.DisplayName : null,
        DueDate = x.DueDate,
        IsCompleted = x.IsCompleted
    };

    private static readonly Func<MeetingDecision, MeetingDecisionDto> CompiledToDecisionDto = ToDecisionDtoExpression.Compile();

    /// <summary>A loaded meeting (with groups, attendees, decisions and attachments included) as a Dto. VersionInfo is set by the caller.</summary>
    public static MeetingDto ToDto(this Meeting entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        MeetingDateUtc = entity.MeetingDateUtc,
        Location = entity.Location,
        Status = entity.Status,
        AgendaNotes = entity.AgendaNotes,
        MinutesNotes = entity.MinutesNotes,
        Groups = entity.Groups
            .OrderBy(g => g.PersonGroup.Name)
            .Select(g => new MeetingGroupDto { GroupId = g.PersonGroupId, GroupName = g.PersonGroup.Name })
            .ToList(),
        Attendees = entity.Attendees
            .OrderBy(a => a.ApplicationUser.DisplayName)
            .Select(a => new MeetingAttendeeDto { UserId = a.ApplicationUserId, DisplayName = a.ApplicationUser.DisplayName, HasAttended = a.HasAttended })
            .ToList(),
        Decisions = entity.Decisions.OrderBy(d => d.CreatedAt).Select(d => d.ToDto()).ToList(),
        Attachments = entity.Attachments.OrderByDescending(a => a.CreatedAt).Select(a => a.ToDto()).ToList()
    };

    public static MeetingDecisionDto ToDto(this MeetingDecision entity) => CompiledToDecisionDto(entity);

    public static MeetingAttachmentDto ToDto(this MeetingAttachment entity) => new()
    {
        Id = entity.Id,
        FileId = entity.FileMetadataId,
        OriginalFileName = entity.FileMetadata.OriginalFileName,
        ContentType = entity.FileMetadata.ContentType,
        FileSizeBytes = entity.FileMetadata.FileSizeBytes,
        IsRecording = entity.IsRecording,
        CreatedAt = entity.CreatedAt,
        TranscriptionStatus = entity.TranscriptionStatus,
        TranscriptionError = entity.TranscriptionError
    };

    public static Meeting ToEntity(this CreateMeetingDto dto)
    {
        var meeting = new Meeting
        {
            Title = dto.Title.Trim(),
            MeetingDateUtc = dto.MeetingDateUtc,
            Location = NullIfBlank(dto.Location),
            AgendaNotes = dto.AgendaNotes
        };

        foreach (var groupId in dto.GroupIds.Distinct())
            meeting.Groups.Add(new MeetingGroup { PersonGroupId = groupId });
        foreach (var userId in dto.AttendeeUserIds.Distinct())
            meeting.Attendees.Add(new MeetingAttendee { ApplicationUserId = userId, HasAttended = false });

        return meeting;
    }

    /// <summary>Sets the fields and adds the new groups/attendees. The caller removes the old ones first and passes the old attendance.</summary>
    public static void ApplyTo(this UpdateMeetingDto dto, Meeting entity, IReadOnlyDictionary<string, bool> previousAttendance)
    {
        entity.Title = dto.Title.Trim();
        entity.MeetingDateUtc = dto.MeetingDateUtc;
        entity.Location = NullIfBlank(dto.Location);
        entity.AgendaNotes = dto.AgendaNotes;
        entity.Status = dto.Status;

        foreach (var groupId in dto.GroupIds.Distinct())
            entity.Groups.Add(new MeetingGroup { PersonGroupId = groupId });
        foreach (var userId in dto.AttendeeUserIds.Distinct())
            entity.Attendees.Add(new MeetingAttendee { ApplicationUserId = userId, HasAttended = previousAttendance.GetValueOrDefault(userId) });
    }

    public static MeetingDecision ToEntity(this CreateMeetingDecisionDto dto, int meetingId) => new()
    {
        MeetingId = meetingId,
        Description = dto.Description.Trim(),
        ResponsibleUserId = NullIfBlank(dto.ResponsibleUserId),
        DueDate = dto.DueDate
    };

    public static void ApplyTo(this UpdateMeetingDecisionDto dto, MeetingDecision entity)
    {
        entity.Description = dto.Description.Trim();
        entity.ResponsibleUserId = NullIfBlank(dto.ResponsibleUserId);
        entity.DueDate = dto.DueDate;
        entity.IsCompleted = dto.IsCompleted;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
