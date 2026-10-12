namespace web.Managers.Communication.Dtos;

public class CommunicationRecipientPageDto
{
    public List<CommunicationRecipientRowDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool ViaEmail { get; set; }
    public bool ViaSms { get; set; }
    public bool IsSent { get; set; }
}
