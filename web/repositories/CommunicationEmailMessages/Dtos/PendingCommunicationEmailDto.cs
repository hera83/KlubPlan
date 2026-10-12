namespace web.Repositories.CommunicationEmailMessages.Dtos;

public class PendingCommunicationEmailDto
{
    public int Id { get; set; }
    public string ToAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }

    /// <summary>FileMetadata ids of the message's attachments.</summary>
    public List<int> AttachmentFileIds { get; set; } = [];
}
