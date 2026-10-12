namespace web.Repositories.LabelCollections.Dtos;

/// <summary>Outcome of removing an image from the library. FileId is the stored file to delete (when Removed).</summary>
public class RemoveLabelCollectionMediaResultDto
{
    public RemoveLabelCollectionMediaStatus Status { get; set; }
    public int FileId { get; set; }
}
