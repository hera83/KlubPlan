using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Repositories.CommunicationEmailMessages.Dtos;
using web.Repositories.CommunicationEmailMessages.Interfaces;
using web.Repositories.CommunicationEmailMessages.Mappers;

namespace web.Repositories.CommunicationEmailMessages;

public class CommunicationEmailMessageRepository(WebDbContext dbContext) : ICommunicationEmailMessageRepository
{
    public Task<List<PendingCommunicationEmailDto>> GetPendingAsync(CancellationToken cancellationToken = default) =>
        dbContext.CommunicationEmailMessages.AsNoTracking()
            .Where(m => m.Status == CommunicationEmailMessageStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Select(CommunicationEmailMessageMapper.ToPendingDtoExpression)
            .ToListAsync(cancellationToken);

    public async Task QueueAsync(IReadOnlyCollection<QueueCommunicationEmailDto> emails, CancellationToken cancellationToken = default)
    {
        dbContext.CommunicationEmailMessages.AddRange(emails.Select(e => e.ToEntity()));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> MarkSentAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.CommunicationEmailMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return false;
        }

        message.Status = CommunicationEmailMessageStatus.Sent;
        message.SentAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarkFailedAsync(int id, string failureReason, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.CommunicationEmailMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return false;
        }

        message.Status = CommunicationEmailMessageStatus.Failed;
        message.FailureReason = failureReason;
        message.FailedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
