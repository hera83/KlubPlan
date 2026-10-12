using web.Repositories.Forms.Dtos;
using web.Repositories.FormSubmissions.Dtos;

namespace web.Managers.Forms.Dtos;

/// <summary>A page of a form's responses: one column per answerable field and one row per submission (newest first).</summary>
public class FormResponsesDto
{
    public int FormId { get; set; }
    public string FormTitle { get; set; } = string.Empty;
    public List<FormResponseColumnDto> Columns { get; set; } = [];
    public FormVersionInfoDto VersionInfo { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<FormSubmissionDto> Rows { get; set; } = [];
}
