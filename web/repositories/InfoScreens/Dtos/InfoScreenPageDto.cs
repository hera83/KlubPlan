namespace web.Repositories.InfoScreens.Dtos;

/// <summary>One page of the screens table. Page/PageSize are the values actually used (after clamping).</summary>
public class InfoScreenPageDto
{
    public List<InfoScreenListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
