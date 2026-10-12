using web.Constants;

namespace web.Repositories.CommunicationMessages.Dtos;

/// <summary>One address a message was sent to, with the delivery status of its queued SMS/e-mail.</summary>
public class CommunicationMessageRecipientDto
{
    public int PersonId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    /// <summary>Null when the queued e-mail no longer exists (or the channel is SMS).</summary>
    public CommunicationEmailMessageStatus? EmailStatus { get; set; }

    /// <summary>Null when the queued SMS no longer exists (or the channel is e-mail).</summary>
    public string? SmsStatus { get; set; }
}
