namespace web.Repositories.CommunicationMessages.Dtos;

public class MarkCommunicationMessageSentDto
{
    public int MessageId { get; set; }
    public List<QueuedCommunicationRecipientDto> Recipients { get; set; } = [];

    /// <summary>New recipient stats — null keeps the current ones (a partial "Send igen").</summary>
    public int? RecipientCount { get; set; }
    public string? RecipientSummary { get; set; }
}
