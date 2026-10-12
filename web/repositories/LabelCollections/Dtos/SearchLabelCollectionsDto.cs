namespace web.Repositories.LabelCollections.Dtos;

public class SearchLabelCollectionsDto
{
    /// <summary>Case-insensitive substring of the name or of any label's stored elements (texts).</summary>
    public string? SearchText { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
