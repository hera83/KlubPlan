using web.Repositories.LabelCollections.Dtos;

namespace web.Managers.LabelCollections.Dtos;

public class UploadLabelMediaResultDto
{
    public UploadLabelMediaStatus Status { get; set; }

    /// <summary>Set when Status is Invalid.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>The new image in the library (when Uploaded).</summary>
    public LabelMediaDto? Media { get; set; }
}
