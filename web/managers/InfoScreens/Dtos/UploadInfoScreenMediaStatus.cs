namespace web.Managers.InfoScreens.Dtos;

public enum UploadInfoScreenMediaStatus
{
    Uploaded,
    NotFound,

    /// <summary>Wrong file type, empty or too big. ErrorMessage says which.</summary>
    Invalid
}
