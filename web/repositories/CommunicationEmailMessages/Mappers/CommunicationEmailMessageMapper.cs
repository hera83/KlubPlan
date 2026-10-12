using System.Linq.Expressions;
using web.Constants;
using web.Data.Models;
using web.Repositories.CommunicationEmailMessages.Dtos;

namespace web.Repositories.CommunicationEmailMessages.Mappers;

public static class CommunicationEmailMessageMapper
{
    public static readonly Expression<Func<CommunicationEmailMessage, PendingCommunicationEmailDto>> ToPendingDtoExpression = x => new PendingCommunicationEmailDto
    {
        Id = x.Id,
        ToAddress = x.ToAddress,
        Subject = x.Subject,
        Body = x.Body,
        HtmlBody = x.HtmlBody,
        AttachmentFileIds = x.CommunicationMessage != null
            ? x.CommunicationMessage.Attachments.Select(a => a.FileMetadataId).ToList()
            : new List<int>()
    };

    public static CommunicationEmailMessage ToEntity(this QueueCommunicationEmailDto dto) => new()
    {
        ToAddress = dto.ToAddress,
        Subject = dto.Subject,
        Body = dto.Body,
        HtmlBody = dto.HtmlBody,
        Status = CommunicationEmailMessageStatus.Pending
    };
}
