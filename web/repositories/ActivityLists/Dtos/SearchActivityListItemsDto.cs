namespace web.Repositories.ActivityLists.Dtos;

public class SearchActivityListItemsDto
{
    public int ListId { get; set; }
    public string? SearchText { get; set; }
    public int? StatusId { get; set; }

    /// <summary>"mine", "none" or a member id.</summary>
    public string? Assigned { get; set; }

    /// <summary>"with" or "without".</summary>
    public string? NoteFilter { get; set; }

    /// <summary>"status", "assigned", "note", "c{columnId}" or empty for the line number.</summary>
    public string? SortColumn { get; set; }
    public string? SortDir { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool HasFilter => !string.IsNullOrWhiteSpace(SearchText) || StatusId.HasValue
        || !string.IsNullOrWhiteSpace(Assigned) || !string.IsNullOrWhiteSpace(NoteFilter);
}
