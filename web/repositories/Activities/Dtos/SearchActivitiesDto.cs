namespace web.Repositories.Activities.Dtos;

public class SearchActivitiesDto
{
    /// <summary>Matches title, location or category.</summary>
    public string? SearchText { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
