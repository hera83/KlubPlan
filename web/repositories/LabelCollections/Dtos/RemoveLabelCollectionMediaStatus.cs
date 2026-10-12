namespace web.Repositories.LabelCollections.Dtos;

public enum RemoveLabelCollectionMediaStatus
{
    Removed,
    NotFound,

    /// <summary>A saved label still uses the image.</summary>
    InUse
}
