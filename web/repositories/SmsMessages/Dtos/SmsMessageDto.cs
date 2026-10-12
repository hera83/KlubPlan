namespace web.Repositories.SmsMessages.Dtos;

public class SmsMessageDto
{
    public int Id { get; set; }
    public Guid? GatewayMessageId { get; set; }

    /// <summary>SmsDirection.Outbound or Inbound.</summary>
    public string Direction { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int SegmentCount { get; set; }
    public decimal? TotalPriceDkk { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
