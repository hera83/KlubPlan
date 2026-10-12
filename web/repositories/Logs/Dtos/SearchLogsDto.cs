namespace web.Repositories.Logs.Dtos;

public class SearchLogsDto
{
    public string? Level { get; set; }
    public string? SearchText { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
