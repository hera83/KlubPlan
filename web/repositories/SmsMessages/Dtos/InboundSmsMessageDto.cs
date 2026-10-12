namespace web.Repositories.SmsMessages.Dtos;

public class InboundSmsMessageDto
{
    public Guid GatewayMessageId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ReceivedAtUtc { get; set; }
}
