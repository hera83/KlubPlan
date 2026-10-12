namespace web.Repositories.LabelCollections.Dtos;

/// <summary>One designed label and how many copies of it to print.</summary>
public class LabelDesignDto
{
    public int Quantity { get; set; } = 1;
    public string Background { get; set; } = "#ffffff";
    public List<LabelElementDto> Elements { get; set; } = new();
}
