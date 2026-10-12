namespace web.Repositories.SmsMessages.Dtos;

/// <summary>What the SMS gateway reported for an outbound message (on submit or when polled).</summary>
public class SmsGatewayStatusDto
{
    /// <summary>Only set on submit.</summary>
    public Guid? GatewayMessageId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SegmentCount { get; set; }
    public decimal? UnitPriceDkk { get; set; }
    public decimal? TotalPriceDkk { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }
    public string? FailureReason { get; set; }
}
