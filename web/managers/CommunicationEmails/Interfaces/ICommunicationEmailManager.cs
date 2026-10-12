namespace web.Managers.CommunicationEmails.Interfaces;

/// <summary>Sends the outgoing e-mail queue from Kommunikation — run by CommunicationEmailWorker.</summary>
public interface ICommunicationEmailManager
{
    /// <summary>Sends every pending e-mail with its message's attachments and marks each one sent or failed.</summary>
    Task SendPendingAsync(CancellationToken cancellationToken = default);
}
