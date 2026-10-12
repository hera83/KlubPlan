using web.Repositories.CommunicationMessages.Dtos;

namespace web.Managers.Communication.Dtos;

public class CommunicationMessageDetailsDto
{
    public CommunicationMessageDto Message { get; set; } = new();
    public int TotalGroupCount { get; set; }

    /// <summary>First page of the recipients table without filters.</summary>
    public CommunicationRecipientPageDto Recipients { get; set; } = new();
}
