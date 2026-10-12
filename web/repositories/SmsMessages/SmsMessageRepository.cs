using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Repositories.SmsMessages.Dtos;
using web.Repositories.SmsMessages.Interfaces;
using web.Repositories.SmsMessages.Mappers;

namespace web.Repositories.SmsMessages;

public class SmsMessageRepository(WebDbContext dbContext) : ISmsMessageRepository
{
    public async Task<SmsMessagePageDto> SearchAsync(SearchSmsMessagesDto search, CancellationToken cancellationToken = default)
    {
        var page = search.Page < 1 ? 1 : search.Page;
        var pageSize = search.PageSize is < 5 or > 200 ? 10 : search.PageSize;

        var query = dbContext.SmsMessages.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search.Direction))
        {
            query = query.Where(m => m.Direction == search.Direction);
        }

        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var term = search.SearchText.Trim();
            query = query.Where(m => m.PhoneNumber.Contains(term) || m.Body.Contains(term) || m.Status.Contains(term));
        }

        return new SmsMessagePageDto
        {
            TotalCount = await query.CountAsync(cancellationToken),
            Page = page,
            PageSize = pageSize,
            Items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(SmsMessageMapper.ToDtoExpression)
                .ToListAsync(cancellationToken)
        };
    }

    public Task<SmsMessageDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.SmsMessages.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(SmsMessageMapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task QueueAsync(IReadOnlyCollection<QueueSmsMessageDto> messages, CancellationToken cancellationToken = default)
    {
        dbContext.SmsMessages.AddRange(messages.Select(m => m.ToEntity()));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RequeueSmsMessageStatus> RequeueAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.SmsMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return RequeueSmsMessageStatus.NotFound;
        }

        if (message.Direction != SmsDirection.Outbound || message.Status != SmsMessageStatus.Failed)
        {
            return RequeueSmsMessageStatus.NotFailed;
        }

        message.GatewayMessageId = null;
        message.Status = SmsMessageStatus.Pending;
        message.FailureReason = null;
        message.FailedAtUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return RequeueSmsMessageStatus.Requeued;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.SmsMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return false;
        }

        dbContext.SmsMessages.Remove(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<List<SmsMessageDto>> GetUnsubmittedAsync(CancellationToken cancellationToken = default) =>
        dbContext.SmsMessages.AsNoTracking()
            .Where(m => m.Direction == SmsDirection.Outbound && m.GatewayMessageId == null)
            .OrderBy(m => m.CreatedAt)
            .Select(SmsMessageMapper.ToDtoExpression)
            .ToListAsync(cancellationToken);

    public async Task ApplySubmitResultAsync(int id, SmsGatewayStatusDto? result, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.SmsMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return;
        }

        if (result is not null)
        {
            message.GatewayMessageId = result.GatewayMessageId;
            message.Status = result.Status;
            message.SegmentCount = result.SegmentCount;
            message.UnitPriceDkk = result.UnitPriceDkk;
            message.TotalPriceDkk = result.TotalPriceDkk;
            message.SentAtUtc = DateTime.UtcNow;
        }

        dbContext.Entry(message).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(int id, string failureReason, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.SmsMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return;
        }

        message.Status = SmsMessageStatus.Failed;
        message.FailureReason = failureReason;
        message.FailedAtUtc = DateTime.UtcNow;
        dbContext.Entry(message).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<SmsMessageDto>> GetInFlightAsync(DateTime createdSinceUtc, CancellationToken cancellationToken = default) =>
        dbContext.SmsMessages.AsNoTracking()
            .Where(m => m.Direction == SmsDirection.Outbound
                && m.GatewayMessageId != null
                && m.FailedAtUtc == null
                && m.Status != SmsMessageStatus.Sent
                && m.CreatedAt >= createdSinceUtc)
            .OrderBy(m => m.CreatedAt)
            .Select(SmsMessageMapper.ToDtoExpression)
            .ToListAsync(cancellationToken);

    public async Task ApplyStatusAsync(int id, SmsGatewayStatusDto status, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.SmsMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return;
        }

        message.Status = status.Status;
        message.SegmentCount = status.SegmentCount;
        message.UnitPriceDkk = status.UnitPriceDkk;
        message.TotalPriceDkk = status.TotalPriceDkk;
        if (status.SentAtUtc.HasValue)
        {
            message.SentAtUtc = status.SentAtUtc;
        }

        if (status.FailedAtUtc.HasValue)
        {
            message.FailedAtUtc = status.FailedAtUtc;
            message.FailureReason = status.FailureReason;
        }

        dbContext.Entry(message).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> AddInboundAsync(IReadOnlyCollection<InboundSmsMessageDto> messages, CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
        {
            return 0;
        }

        var incomingIds = messages.Select(m => m.GatewayMessageId).ToList();
        var existingIds = (await dbContext.SmsMessages
                .Where(m => m.Direction == SmsDirection.Inbound && m.GatewayMessageId != null && incomingIds.Contains(m.GatewayMessageId.Value))
                .Select(m => m.GatewayMessageId!.Value)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var newMessages = messages.Where(m => !existingIds.Contains(m.GatewayMessageId)).Select(m => m.ToEntity()).ToList();
        if (newMessages.Count == 0)
        {
            return 0;
        }

        dbContext.SmsMessages.AddRange(newMessages);
        await dbContext.SaveChangesAsync(cancellationToken);
        return newMessages.Count;
    }
}
