namespace web.Data.Models;

/// <summary>
/// An image uploaded to a LabelCollection (its image library). The file lives under app_files/labels/;
/// label elements refer to it by PublicId.
/// </summary>
public class LabelCollectionMedia : EntityBase
{
    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int LabelCollectionId { get; set; }

    public int FileMetadataId { get; set; }

    public virtual LabelCollection Collection { get; set; } = null!;

    public virtual FileMetadata FileMetadata { get; set; } = null!;
}
