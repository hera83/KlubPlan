using web.Constants;
using web.Repositories.Arrangements.Dtos;

namespace web.Managers.Communication.Dtos;

public class ComposeArrangementOptionDto
{
    public ArrangementLinkOptionDto Arrangement { get; set; } = new();
    public ArrangementRegistrationStatus Status { get; set; }
}
