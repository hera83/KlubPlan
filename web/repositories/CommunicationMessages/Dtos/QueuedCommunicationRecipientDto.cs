namespace web.Repositories.CommunicationMessages.Dtos;

/// <summary>One resolved send target with its personalized text — becomes a recipient row plus a queued SMS or e-mail.</summary>
public class QueuedCommunicationRecipientDto
{
    public int PersonId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>Only used for e-mail.</summary>
    public string? HtmlBody { get; set; }
}
