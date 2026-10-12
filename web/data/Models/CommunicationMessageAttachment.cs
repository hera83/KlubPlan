namespace web.Data.Models;

/// <summary>
/// Links a CommunicationMessage to an uploaded file, sent as a mail attachment when the message
/// is delivered via e-mail (SMS has no concept of attachments). The physical file and its
/// metadata live in FileMetadata as usual — this just associates it with the message.
/// </summary>
public class CommunicationMessageAttachment : EntityBase
{
    public int CommunicationMessageId { get; set; }

    public int FileMetadataId { get; set; }

    public virtual CommunicationMessage CommunicationMessage { get; set; } = null!;

    public virtual FileMetadata FileMetadata { get; set; } = null!;
}
