namespace web.Managers.Communication.Dtos;

public class SaveCommunicationDto
{
    /// <summary>0 = new message.</summary>
    public int Id { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public bool ViaEmail { get; set; }
    public bool ViaSms { get; set; }
    public List<int> GroupIds { get; set; } = [];
    public List<int> PersonIds { get; set; } = [];
    public int? FormId { get; set; }

    /// <summary>"shared" or "personal".</summary>
    public string? LinkType { get; set; }
    public int? ArrangementId { get; set; }
    public int? ActivityId { get; set; }

    /// <summary>True = send right after saving the draft.</summary>
    public bool Send { get; set; }
    public string? UserId { get; set; }

    /// <summary>Only stored when ViaEmail is set — SMS has no attachments.</summary>
    public List<CommunicationAttachmentUploadDto> Attachments { get; set; } = [];
}
