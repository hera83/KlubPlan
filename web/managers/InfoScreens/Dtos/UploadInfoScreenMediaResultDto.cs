using web.Repositories.InfoScreens.Dtos;

namespace web.Managers.InfoScreens.Dtos;

public class UploadInfoScreenMediaResultDto
{
    public UploadInfoScreenMediaStatus Status { get; set; }

    /// <summary>Set when Status is Invalid.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>The new file in the media library (when Uploaded).</summary>
    public InfoScreenMediaDto? Media { get; set; }
}
