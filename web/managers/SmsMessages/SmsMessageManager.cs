using web.Constants;
using web.Managers.SmsMessages.Dtos;
using web.Managers.SmsMessages.Interfaces;
using web.Repositories.SmsMessages.Dtos;
using web.Repositories.SmsMessages.Interfaces;
using web.Services.Sms.Dtos.Sms;
using web.Services.Sms.Interfaces;

namespace web.Managers.SmsMessages;

public class SmsMessageManager(
    ISmsMessageRepository smsMessageRepository,
    ISmsService smsService,
    ILogger<SmsMessageManager> logger) : ISmsMessageManager
{
    private static readonly TimeSpan PollWindow = TimeSpan.FromDays(7);

    public async Task SendOutboundAsync(CancellationToken cancellationToken = default)
    {
        foreach (var message in await smsMessageRepository.GetUnsubmittedAsync(cancellationToken))
        {
            try
            {
                var response = await smsService.SendSmsAsync(
                    new SendSmsRequestDto { To = message.PhoneNumber, Message = message.Body },
                    cancellationToken: cancellationToken);

                await smsMessageRepository.ApplySubmitResultAsync(message.Id, response is null ? null : new SmsGatewayStatusDto
                {
                    GatewayMessageId = response.MessageId,
                    Status = response.Status,
                    SegmentCount = response.SegmentCount,
                    UnitPriceDkk = response.UnitPriceDkk,
                    TotalPriceDkk = response.TotalPriceDkk
                }, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Failed to send SMS {SmsMessageId}", message.Id);
                await smsMessageRepository.MarkFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }

    public async Task PollOutboundStatusesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var message in await smsMessageRepository.GetInFlightAsync(DateTime.UtcNow - PollWindow, cancellationToken))
        {
            try
            {
                var status = await smsService.GetSmsStatusAsync(
                    new GetStatusSmsRequestDto { MessageId = message.GatewayMessageId!.Value },
                    cancellationToken: cancellationToken);
                if (status is null)
                {
                    continue;
                }

                await smsMessageRepository.ApplyStatusAsync(message.Id, new SmsGatewayStatusDto
                {
                    Status = status.Status,
                    SegmentCount = status.SegmentCount,
                    UnitPriceDkk = status.UnitPriceDkk,
                    TotalPriceDkk = status.TotalPriceDkk,
                    SentAtUtc = status.SentAt,
                    FailedAtUtc = status.FailedAt,
                    FailureReason = status.FailureReason
                }, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Failed to poll SMS status for {SmsMessageId} (gateway id {GatewayMessageId})", message.Id, message.GatewayMessageId);
            }
        }
    }

    public async Task FetchInboundAsync(CancellationToken cancellationToken = default)
    {
        var incoming = await smsService.ReadSmsAsync(cancellationToken: cancellationToken);
        var added = await smsMessageRepository.AddInboundAsync(incoming.Select(m => new InboundSmsMessageDto
        {
            GatewayMessageId = m.Id,
            PhoneNumber = m.FromPhoneNumber ?? string.Empty,
            Body = m.Body,
            Status = m.Status,
            ReceivedAtUtc = m.ReceivedAt
        }).ToList(), cancellationToken);

        if (added > 0)
        {
            logger.LogInformation("Registered {Count} new inbound SMS messages", added);
        }
    }

    public async Task<DeleteSmsMessageStatus> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await smsMessageRepository.GetByIdAsync(id, cancellationToken);
        if (message is null)
        {
            return DeleteSmsMessageStatus.NotFound;
        }

        if (message.Status != SmsMessageStatus.Queued && message.Status != SmsMessageStatus.Failed)
        {
            return DeleteSmsMessageStatus.NotDeletable;
        }

        if (message.Status == SmsMessageStatus.Queued && message.GatewayMessageId.HasValue)
        {
            try
            {
                await smsService.DeleteSmsAsync(message.GatewayMessageId.Value, cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning(ex, "Could not cancel SMS {SmsMessageId} (gateway id {GatewayMessageId}) in the SMS gateway", message.Id, message.GatewayMessageId);
                return DeleteSmsMessageStatus.GatewayFailed;
            }
        }

        return await smsMessageRepository.DeleteAsync(id, cancellationToken)
            ? DeleteSmsMessageStatus.Deleted
            : DeleteSmsMessageStatus.NotFound;
    }
}
