namespace web.Repositories.LabelCollections.Dtos;

/// <summary>
/// Everything the label designer edits and saves in one go ("Gem"): the collection's name, its sheet
/// grid and all its labels. Sent as JSON (camelCase, LabelCollectionMapper.JsonOptions) both ways;
/// the server validates and normalizes it on save.
/// </summary>
public class LabelCollectionDesignDto
{
    public string Name { get; set; } = string.Empty;
    public int Across { get; set; } = 3;
    public int Down { get; set; } = 8;
    public bool Landscape { get; set; }
    public List<LabelDesignDto> Labels { get; set; } = new();
}
