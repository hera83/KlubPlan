namespace web.Repositories.CommunicationMessages.Dtos;

/// <summary>A draft to create (Id = 0) or update. Recipients and the summary are already resolved by the caller.</summary>
public class SaveCommunicationMessageDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool ViaEmail { get; set; }
    public bool ViaSms { get; set; }
    public int? FormId { get; set; }
    public string? LinkType { get; set; }
    public int? ArrangementId { get; set; }
    public int? ActivityId { get; set; }
    public List<int> GroupIds { get; set; } = [];
    public List<int> PersonIds { get; set; } = [];
    public int RecipientCount { get; set; }
    public string RecipientSummary { get; set; } = string.Empty;
    public string? CreatedByUserId { get; set; }
}
