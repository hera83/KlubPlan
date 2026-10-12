using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Models;
using web.Repositories.CommunicationMessages.Dtos;
using web.Repositories.CommunicationMessages.Interfaces;
using web.Repositories.CommunicationMessages.Mappers;

namespace web.Repositories.CommunicationMessages;

public class CommunicationMessageRepository(WebDbContext dbContext) : ICommunicationMessageRepository
{
    public Task<List<CommunicationMessageListItemDto>> GetListAsync(int? activityId, CancellationToken cancellationToken = default) =>
        dbContext.CommunicationMessages.AsNoTracking()
            .Where(m => activityId == null || m.ActivityId == activityId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(CommunicationMessageMapper.ToListItemDtoExpression)
            .ToListAsync(cancellationToken);

    public Task<CommunicationMessageDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.CommunicationMessages.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(CommunicationMessageMapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<CommunicationMessageRecipientDto>> GetRecipientsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.CommunicationMessageRecipients.AsNoTracking()
            .Where(r => r.CommunicationMessageId == id)
            .OrderBy(r => r.Id)
            .Select(CommunicationMessageMapper.ToRecipientDtoExpression)
            .ToListAsync(cancellationToken);

    public async Task<SaveCommunicationMessageResultDto> SaveDraftAsync(SaveCommunicationMessageDto dto, CancellationToken cancellationToken = default)
    {
        CommunicationMessage message;
        if (dto.Id > 0)
        {
            var existing = await dbContext.CommunicationMessages
                .Include(m => m.Groups)
                .Include(m => m.DirectPersons)
                .FirstOrDefaultAsync(m => m.Id == dto.Id, cancellationToken);
            if (existing is null)
            {
                return new SaveCommunicationMessageResultDto { Status = SaveCommunicationMessageStatus.NotFound };
            }

            if (existing.Status != CommunicationMessageStatus.Draft)
            {
                return new SaveCommunicationMessageResultDto { Status = SaveCommunicationMessageStatus.NotDraft };
            }

            dbContext.CommunicationMessageGroups.RemoveRange(existing.Groups);
            dbContext.CommunicationMessageRecipientPersons.RemoveRange(existing.DirectPersons);
            existing.Groups.Clear();
            existing.DirectPersons.Clear();
            dto.ApplyTo(existing);
            // Touch UpdatedAt even when only the selections changed.
            dbContext.Entry(existing).State = EntityState.Modified;
            message = existing;
        }
        else
        {
            message = dto.ToEntity();
            dbContext.CommunicationMessages.Add(message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new SaveCommunicationMessageResultDto { Status = SaveCommunicationMessageStatus.Saved, Id = message.Id };
    }

    public async Task<bool> AddAttachmentAsync(int messageId, int fileMetadataId, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.CommunicationMessages.AnyAsync(m => m.Id == messageId, cancellationToken))
        {
            return false;
        }

        dbContext.CommunicationMessageAttachments.Add(new CommunicationMessageAttachment
        {
            CommunicationMessageId = messageId,
            FileMetadataId = fileMetadataId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<int?> GetAttachmentFileIdAsync(int attachmentId, CancellationToken cancellationToken = default) =>
        dbContext.CommunicationMessageAttachments.AsNoTracking()
            .Where(a => a.Id == attachmentId)
            .Select(a => (int?)a.FileMetadataId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> MarkSentAsync(MarkCommunicationMessageSentDto dto, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.CommunicationMessages.FirstOrDefaultAsync(m => m.Id == dto.MessageId, cancellationToken);
        if (message is null)
        {
            return false;
        }

        foreach (var recipient in dto.Recipients)
        {
            var recipientRow = new CommunicationMessageRecipient
            {
                CommunicationMessageId = message.Id,
                PersonId = recipient.PersonId,
                DisplayName = recipient.DisplayName,
                Channel = recipient.Channel,
                Address = recipient.Address
            };

            if (recipient.Channel == CommunicationChannel.Sms)
            {
                var sms = new SmsMessage
                {
                    Direction = SmsDirection.Outbound,
                    PhoneNumber = recipient.Address,
                    Body = recipient.Body,
                    Status = SmsMessageStatus.Pending
                };
                dbContext.SmsMessages.Add(sms);
                recipientRow.SmsMessage = sms;
            }
            else
            {
                var email = new CommunicationEmailMessage
                {
                    CommunicationMessageId = message.Id,
                    ToAddress = recipient.Address,
                    Subject = message.Subject,
                    Body = recipient.Body,
                    HtmlBody = recipient.HtmlBody,
                    Status = CommunicationEmailMessageStatus.Pending
                };
                dbContext.CommunicationEmailMessages.Add(email);
                recipientRow.CommunicationEmailMessage = email;
            }

            dbContext.CommunicationMessageRecipients.Add(recipientRow);
        }

        message.Status = CommunicationMessageStatus.Sent;
        message.SentAtUtc = DateTime.UtcNow;
        if (dto.RecipientCount.HasValue)
        {
            message.RecipientCount = dto.RecipientCount.Value;
        }

        if (dto.RecipientSummary is not null)
        {
            message.RecipientSummary = dto.RecipientSummary;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<int>?> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.CommunicationMessages
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return null;
        }

        var fileIds = message.Attachments.Select(a => a.FileMetadataId).ToList();

        // Groups/DirectPersons/Recipients/Attachments cascade-delete with the message.
        dbContext.CommunicationMessages.Remove(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        return fileIds;
    }
}
