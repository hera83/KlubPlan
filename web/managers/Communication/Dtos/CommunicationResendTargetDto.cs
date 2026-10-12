namespace web.Managers.Communication.Dtos;

public class CommunicationResendTargetDto
{
    public int PersonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool ReceivedBefore { get; set; }
}
