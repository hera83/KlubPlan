namespace web.Repositories.Arrangements.Dtos;

/// <summary>One page of the arrangements table. Page/PageSize are the values actually used (after clamping).</summary>
public class ArrangementPageDto
{
    public List<ArrangementListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
