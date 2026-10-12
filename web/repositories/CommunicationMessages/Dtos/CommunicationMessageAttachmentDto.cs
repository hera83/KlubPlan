namespace web.Repositories.CommunicationMessages.Dtos;

public class CommunicationMessageAttachmentDto
{
    public int Id { get; set; }
    public int FileMetadataId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}
