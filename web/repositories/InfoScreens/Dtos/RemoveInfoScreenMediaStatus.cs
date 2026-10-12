namespace web.Repositories.InfoScreens.Dtos;

public enum RemoveInfoScreenMediaStatus
{
    Removed,
    NotFound,

    /// <summary>A saved slide still uses the file.</summary>
    InUse
}
