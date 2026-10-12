using web.Constants;

namespace web.Repositories.CommunicationMessages.Dtos;

public class CommunicationMessageDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool ViaEmail { get; set; }
    public bool ViaSms { get; set; }
    public CommunicationMessageStatus Status { get; set; }
    public string RecipientSummary { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
    public int? FormId { get; set; }
    public string? FormTitle { get; set; }

    /// <summary>"shared" or "personal" — only set when FormId is set.</summary>
    public string? LinkType { get; set; }
    public int? ArrangementId { get; set; }
    public string? ArrangementTitle { get; set; }
    public int? ActivityId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public List<CommunicationMessageGroupDto> Groups { get; set; } = [];
    public List<int> DirectPersonIds { get; set; } = [];
    public List<string> DirectPersonNames { get; set; } = [];
    public List<CommunicationMessageAttachmentDto> Attachments { get; set; } = [];
}
