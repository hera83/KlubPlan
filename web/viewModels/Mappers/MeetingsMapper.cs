using web.Extensions;
using web.Managers.Meetings.Dtos;
using web.Repositories.Meetings.Dtos;
using web.Repositories.PersonGroups.Dtos;
using web.Repositories.Users.Dtos;

namespace web.ViewModels.Mappers;

/// <summary>Maps between the Meetings view models and the Meetings repository's and manager's Dtos.</summary>
public static class MeetingsMapper
{
    public static SearchMeetingsDto ToSearchMeetingsDto(this MeetingFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        Status = filter.Status,
        GroupIds = filter.GroupIds,
        Page = filter.Page,
        PageSize = filter.PageSize
    };

    public static MeetingFilterViewModel ToMeetingFilterViewModel(this MeetingsOverviewDto overview, MeetingFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        Status = filter.Status,
        GroupIds = filter.GroupIds,
        Page = overview.Meetings.Page,
        PageSize = overview.Meetings.PageSize,
        TotalCount = overview.Meetings.TotalCount,
        Meetings = overview.Meetings.Items.Select(m => new MeetingListItemViewModel
        {
            Id = m.Id,
            Title = m.Title,
            MeetingDateUtc = m.MeetingDateUtc,
            Location = m.Location,
            Status = m.Status,
            Groups = m.Groups.Select(ToMeetingGroupItemViewModel).ToList(),
            IsAllGroups = GroupDisplayHelper.IsAllGroups(m.Groups.Count, overview.Groups.Count),
            AttendeeCount = m.AttendeeCount,
            AttendedCount = m.AttendedCount,
            VersionNumber = m.VersionNumber,
            VersionCount = m.VersionCount,
            Versions = m.Versions.Select(v => new MeetingVersionOptionViewModel
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                MeetingDateUtc = v.MeetingDateUtc,
                Status = v.Status,
                IsCurrent = v.IsCurrent
            }).ToList()
        }).ToList(),
        Groups = overview.Groups.ToPersonGroupOptionViewModels()
    };

    public static MeetingDetailViewModel ToMeetingDetailViewModel(this MeetingDto meeting, int totalGroupCount) => new()
    {
        Id = meeting.Id,
        VersionNumber = meeting.VersionInfo.VersionNumber,
        VersionCount = meeting.VersionInfo.VersionCount,
        IsLatestVersion = meeting.VersionInfo.IsLatestVersion,
        CurrentVersionId = meeting.VersionInfo.CurrentVersionId,
        PreviousVersionId = meeting.VersionInfo.PreviousVersionId,
        NextVersionId = meeting.VersionInfo.NextVersionId,
        Title = meeting.Title,
        MeetingDateUtc = meeting.MeetingDateUtc,
        Location = meeting.Location,
        Status = meeting.Status,
        Groups = meeting.Groups.Select(ToMeetingGroupItemViewModel).ToList(),
        IsAllGroups = GroupDisplayHelper.IsAllGroups(meeting.Groups.Count, totalGroupCount),
        AgendaNotes = meeting.AgendaNotes,
        MinutesNotes = meeting.MinutesNotes,
        Attendees = meeting.Attendees
            .Select(a => new MeetingAttendeeViewModel { UserId = a.UserId, DisplayName = a.DisplayName, HasAttended = a.HasAttended })
            .ToList(),
        Decisions = meeting.Decisions.Select(ToMeetingDecisionViewModel).ToList(),
        Attachments = meeting.Attachments.Select(ToMeetingAttachmentViewModel).ToList()
    };

    public static MeetingDecisionViewModel ToMeetingDecisionViewModel(this MeetingDecisionDto decision) => new()
    {
        Id = decision.Id,
        Description = decision.Description,
        ResponsibleUserId = decision.ResponsibleUserId,
        ResponsibleUserName = decision.ResponsibleUserName,
        DueDate = decision.DueDate,
        IsCompleted = decision.IsCompleted
    };

    public static MeetingAttachmentViewModel ToMeetingAttachmentViewModel(this MeetingAttachmentDto attachment) => new()
    {
        Id = attachment.Id,
        OriginalFileName = attachment.OriginalFileName,
        ContentType = attachment.ContentType,
        FileSizeBytes = attachment.FileSizeBytes,
        IsRecording = attachment.IsRecording,
        CreatedAtUtc = attachment.CreatedAt,
        TranscriptionStatus = attachment.TranscriptionStatus,
        TranscriptionError = attachment.TranscriptionError
    };

    public static List<MeetingAdminOptionViewModel> ToMeetingAdminOptionViewModels(this List<UserOptionDto> admins) =>
        admins.Select(a => new MeetingAdminOptionViewModel { Id = a.Id, DisplayName = a.DisplayName }).ToList();

    public static List<PersonGroupOptionViewModel> ToPersonGroupOptionViewModels(this List<PersonGroupDto> groups) =>
        groups.Select(g => new PersonGroupOptionViewModel { Id = g.Id, Name = g.Name }).ToList();

    public static CreateMeetingDto ToCreateMeetingDto(this CreateMeetingViewModel model) => new()
    {
        Title = model.Title,
        MeetingDateUtc = model.MeetingDate,
        Location = model.Location,
        GroupIds = model.GroupIds,
        AttendeeUserIds = model.AttendeeUserIds,
        AgendaNotes = model.AgendaNotes
    };

    public static UpdateMeetingDto ToUpdateMeetingDto(this EditMeetingViewModel model) => new()
    {
        Title = model.Title,
        MeetingDateUtc = model.MeetingDate,
        Location = model.Location,
        GroupIds = model.GroupIds,
        AttendeeUserIds = model.AttendeeUserIds,
        AgendaNotes = model.AgendaNotes,
        Status = model.Status
    };

    public static CreateMeetingDecisionDto ToCreateMeetingDecisionDto(this CreateMeetingDecisionViewModel model) => new()
    {
        Description = model.Description,
        ResponsibleUserId = model.ResponsibleUserId,
        DueDate = model.DueDate
    };

    public static UpdateMeetingDecisionDto ToUpdateMeetingDecisionDto(this EditMeetingDecisionViewModel model) => new()
    {
        Description = model.Description,
        ResponsibleUserId = model.ResponsibleUserId,
        DueDate = model.DueDate,
        IsCompleted = model.IsCompleted
    };

    public static TranscriptionStatusViewModel ToTranscriptionStatusViewModel(this TranscriptionStatusDto status) => new()
    {
        AttachmentId = status.AttachmentId,
        Status = status.Status,
        Error = status.Error
    };

    private static MeetingGroupItemViewModel ToMeetingGroupItemViewModel(MeetingGroupDto group) =>
        new() { GroupId = group.GroupId, GroupName = group.GroupName };
}
