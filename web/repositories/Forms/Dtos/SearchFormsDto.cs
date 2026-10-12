namespace web.Repositories.Forms.Dtos;

public class SearchFormsDto
{
    /// <summary>Substring of the title.</summary>
    public string? SearchText { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
