namespace web.Repositories.Meetings.Dtos;

public enum SaveMeetingStatus
{
    Saved,
    NotFound,

    /// <summary>A newer version exists — only the latest version can be changed.</summary>
    NotLatestVersion
}
