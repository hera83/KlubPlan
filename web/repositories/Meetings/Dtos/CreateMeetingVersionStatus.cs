namespace web.Repositories.Meetings.Dtos;

public enum CreateMeetingVersionStatus
{
    Created,
    NotFound,

    /// <summary>The meeting must be held or cancelled first.</summary>
    StillPlanned,

    /// <summary>A newer version already exists.</summary>
    NotLatestVersion
}
