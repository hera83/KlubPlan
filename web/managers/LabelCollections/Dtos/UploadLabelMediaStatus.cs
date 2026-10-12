namespace web.Managers.LabelCollections.Dtos;

public enum UploadLabelMediaStatus
{
    Uploaded,
    NotFound,

    /// <summary>Wrong file type, empty or too big. ErrorMessage says which.</summary>
    Invalid
}
