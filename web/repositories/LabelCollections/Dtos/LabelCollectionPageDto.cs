namespace web.Repositories.LabelCollections.Dtos;

/// <summary>One page of the collections table. Page/PageSize are the values actually used (after clamping).</summary>
public class LabelCollectionPageDto
{
    public List<LabelCollectionListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
