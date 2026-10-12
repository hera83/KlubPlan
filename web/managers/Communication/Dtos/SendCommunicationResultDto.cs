namespace web.Managers.Communication.Dtos;

public class SendCommunicationResultDto
{
    public SendCommunicationStatus Status { get; set; }
    public int MessageId { get; set; }
    public bool Success => Status is SendCommunicationStatus.Saved or SendCommunicationStatus.Sent;
}
