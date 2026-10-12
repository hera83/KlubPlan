namespace web.Repositories.LabelCollections.Dtos;

public class LabelCollectionListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Different label designs in the collection.</summary>
    public int LabelCount { get; set; }

    /// <summary>Labels on the sheets, i.e. the sum of the quantities.</summary>
    public int Copies { get; set; }

    public int Across { get; set; }
    public int Down { get; set; }
    public bool Landscape { get; set; }
    public DateTime UpdatedAt { get; set; }
}
