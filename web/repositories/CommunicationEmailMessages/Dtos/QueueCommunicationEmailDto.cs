namespace web.Repositories.CommunicationEmailMessages.Dtos;

/// <summary>An e-mail to put in the outgoing queue without a message from Kommunikation behind it.</summary>
public class QueueCommunicationEmailDto
{
    public string ToAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
}
