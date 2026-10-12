using web.Constants;

namespace web.Repositories.CommunicationMessages.Dtos;

public class CommunicationMessageListItemDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public bool ViaEmail { get; set; }
    public bool ViaSms { get; set; }
    public CommunicationMessageStatus Status { get; set; }
    public int RecipientCount { get; set; }
    public List<CommunicationMessageGroupDto> Groups { get; set; } = [];
    public List<string> DirectPersonNames { get; set; } = [];
    public DateTime? SentAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
}
