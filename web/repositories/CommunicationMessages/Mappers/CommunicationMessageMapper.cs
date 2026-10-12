using System.Linq.Expressions;
using web.Constants;
using web.Data.Models;
using web.Repositories.CommunicationMessages.Dtos;

namespace web.Repositories.CommunicationMessages.Mappers;

public static class CommunicationMessageMapper
{
    public static readonly Expression<Func<CommunicationMessage, CommunicationMessageDto>> ToDtoExpression = x => new CommunicationMessageDto
    {
        Id = x.Id,
        Subject = x.Subject,
        Body = x.Body,
        ViaEmail = x.ViaEmail,
        ViaSms = x.ViaSms,
        Status = x.Status,
        RecipientSummary = x.RecipientSummary,
        RecipientCount = x.RecipientCount,
        FormId = x.FormId,
        FormTitle = x.Form != null ? x.Form.Title : null,
        LinkType = x.LinkType,
        ArrangementId = x.ArrangementId,
        ArrangementTitle = x.Arrangement != null ? x.Arrangement.Title : null,
        ActivityId = x.ActivityId,
        CreatedAt = x.CreatedAt,
        SentAtUtc = x.SentAtUtc,
        Groups = x.Groups.Select(g => new CommunicationMessageGroupDto { Id = g.PersonGroupId, Name = g.PersonGroup.Name }).ToList(),
        DirectPersonIds = x.DirectPersons.Select(p => p.PersonId).ToList(),
        DirectPersonNames = x.DirectPersons.Select(p => p.Person.Name).ToList(),
        Attachments = x.Attachments.Select(a => new CommunicationMessageAttachmentDto
        {
            Id = a.Id,
            FileMetadataId = a.FileMetadataId,
            OriginalFileName = a.FileMetadata.OriginalFileName,
            FileSizeBytes = a.FileMetadata.FileSizeBytes
        }).ToList()
    };

    public static readonly Expression<Func<CommunicationMessage, CommunicationMessageListItemDto>> ToListItemDtoExpression = x => new CommunicationMessageListItemDto
    {
        Id = x.Id,
        Subject = x.Subject,
        ViaEmail = x.ViaEmail,
        ViaSms = x.ViaSms,
        Status = x.Status,
        RecipientCount = x.RecipientCount,
        Groups = x.Groups.Select(g => new CommunicationMessageGroupDto { Id = g.PersonGroupId, Name = g.PersonGroup.Name }).ToList(),
        DirectPersonNames = x.DirectPersons.Select(p => p.Person.Name).ToList(),
        SentAtUtc = x.SentAtUtc,
        CreatedAt = x.CreatedAt
    };

    public static readonly Expression<Func<CommunicationMessageRecipient, CommunicationMessageRecipientDto>> ToRecipientDtoExpression = x => new CommunicationMessageRecipientDto
    {
        PersonId = x.PersonId,
        Channel = x.Channel,
        Address = x.Address,
        EmailStatus = x.CommunicationEmailMessage != null ? x.CommunicationEmailMessage.Status : null,
        SmsStatus = x.SmsMessage != null ? x.SmsMessage.Status : null
    };

    public static CommunicationMessage ToEntity(this SaveCommunicationMessageDto dto)
    {
        var entity = new CommunicationMessage { CreatedByUserId = dto.CreatedByUserId };
        dto.ApplyTo(entity);
        return entity;
    }

    /// <summary>Sets the draft's fields and adds its group/person selections (the caller removes the old ones first).</summary>
    public static void ApplyTo(this SaveCommunicationMessageDto dto, CommunicationMessage entity)
    {
        entity.Subject = dto.Subject;
        entity.Body = dto.Body;
        entity.ViaEmail = dto.ViaEmail;
        entity.ViaSms = dto.ViaSms;
        entity.FormId = dto.FormId;
        entity.LinkType = dto.LinkType;
        entity.ArrangementId = dto.ArrangementId;
        entity.ActivityId = dto.ActivityId;
        entity.Status = CommunicationMessageStatus.Draft;
        entity.RecipientCount = dto.RecipientCount;
        entity.RecipientSummary = dto.RecipientSummary;

        foreach (var groupId in dto.GroupIds)
        {
            entity.Groups.Add(new CommunicationMessageGroup { PersonGroupId = groupId });
        }

        foreach (var personId in dto.PersonIds)
        {
            entity.DirectPersons.Add(new CommunicationMessageRecipientPerson { PersonId = personId });
        }
    }
}
