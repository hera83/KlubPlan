using System.Linq.Expressions;
using web.Constants;
using web.Data.Models;
using web.Repositories.SmsMessages.Dtos;

namespace web.Repositories.SmsMessages.Mappers;

public static class SmsMessageMapper
{
    public static readonly Expression<Func<SmsMessage, SmsMessageDto>> ToDtoExpression = x => new SmsMessageDto
    {
        Id = x.Id,
        GatewayMessageId = x.GatewayMessageId,
        Direction = x.Direction,
        PhoneNumber = x.PhoneNumber,
        Body = x.Body,
        Status = x.Status,
        SegmentCount = x.SegmentCount,
        TotalPriceDkk = x.TotalPriceDkk,
        FailureReason = x.FailureReason,
        CreatedAt = x.CreatedAt
    };

    public static SmsMessage ToEntity(this QueueSmsMessageDto dto) => new()
    {
        Direction = SmsDirection.Outbound,
        PhoneNumber = dto.PhoneNumber,
        Body = dto.Body,
        Status = SmsMessageStatus.Pending
    };

    public static SmsMessage ToEntity(this InboundSmsMessageDto dto) => new()
    {
        GatewayMessageId = dto.GatewayMessageId,
        Direction = SmsDirection.Inbound,
        PhoneNumber = dto.PhoneNumber,
        Body = dto.Body,
        Status = dto.Status,
        ReceivedAtUtc = dto.ReceivedAtUtc
    };
}
