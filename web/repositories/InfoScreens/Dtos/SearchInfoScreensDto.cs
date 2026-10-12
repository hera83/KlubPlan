namespace web.Repositories.InfoScreens.Dtos;

public class SearchInfoScreensDto
{
    /// <summary>Case-insensitive substring of the title.</summary>
    public string? SearchText { get; set; }

    /// <summary>"active", "inactive" or empty for all.</summary>
    public string? Status { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
