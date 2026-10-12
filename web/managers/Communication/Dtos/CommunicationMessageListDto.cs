using web.Repositories.CommunicationMessages.Dtos;

namespace web.Managers.Communication.Dtos;

public class CommunicationMessageListDto
{
    public List<CommunicationMessageListItemDto> Messages { get; set; } = [];

    /// <summary>Number of groups in the club — a message to all of them shows "Alle".</summary>
    public int TotalGroupCount { get; set; }
}
