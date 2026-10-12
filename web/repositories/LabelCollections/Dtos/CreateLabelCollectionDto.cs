namespace web.Repositories.LabelCollections.Dtos;

public class CreateLabelCollectionDto
{
    public string Name { get; set; } = string.Empty;
    public int Across { get; set; }
    public int Down { get; set; }
    public bool Landscape { get; set; }
}
