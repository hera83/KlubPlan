namespace web.Repositories.Forms.Dtos;

/// <summary>One page of the forms table. Page/PageSize are the values actually used (after clamping).</summary>
public class FormPageDto
{
    public List<FormListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
