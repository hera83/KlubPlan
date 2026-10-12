namespace web.Repositories.LabelCollections.Dtos;

/// <summary>What the designer (and the print preview) opens with: the saved design and the image library (newest first).</summary>
public class LabelCollectionDesignerDto
{
    public int Id { get; set; }
    public LabelCollectionDesignDto Design { get; set; } = new();
    public List<LabelMediaDto> Media { get; set; } = [];
}
