using web.Repositories.ArrangementRegistrations.Dtos;

namespace web.Managers.Arrangements.Dtos;

/// <summary>A page of an arrangement's registrations: one column per answerable sign-up field, newest registration first.</summary>
public class ArrangementRegistrationsDto
{
    public int ArrangementId { get; set; }
    public string ArrangementTitle { get; set; } = string.Empty;
    public List<ArrangementRegistrationColumnDto> Columns { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<ArrangementRegistrationDto> Rows { get; set; } = [];
}
