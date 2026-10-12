namespace web.Repositories.LabelCollections.Dtos;

/// <summary>What the PDF needs: the saved design and, per image id, the stored file behind it.</summary>
public class LabelCollectionPrintDto
{
    public string Name { get; set; } = string.Empty;
    public LabelCollectionDesignDto Design { get; set; } = new();
    public Dictionary<Guid, int> MediaFileIds { get; set; } = [];
}
