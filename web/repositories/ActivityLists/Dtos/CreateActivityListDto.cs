namespace web.Repositories.ActivityLists.Dtos;

/// <summary>A list with its imported columns and lines (an already parsed Excel sheet).</summary>
public class CreateActivityListDto
{
    public int ActivityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SourceFileName { get; set; }
    public List<string> Headers { get; set; } = [];

    /// <summary>One array per line, same length as Headers.</summary>
    public List<string?[]> Rows { get; set; } = [];
    public string? CreatedByUserId { get; set; }
}
