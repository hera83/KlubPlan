namespace web.Managers.Communication.Dtos;

public class CommunicationAttachmentUploadDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public Stream Content { get; set; } = Stream.Null;
}
