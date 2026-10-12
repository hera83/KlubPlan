using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.ActivityFiles.Dtos;

namespace web.Repositories.ActivityFiles.Mappers;

public static class ActivityFileMapper
{
    /// <summary>A version as the versions dialog shows it. IsCurrent is set by the caller (newest = current).</summary>
    public static readonly Expression<Func<ActivityFileVersion, ActivityFileVersionItemDto>> ToVersionItemDtoExpression = x => new ActivityFileVersionItemDto
    {
        Id = x.Id,
        VersionNumber = x.VersionNumber,
        SizeBytes = x.FileMetadata.FileSizeBytes,
        ContentType = x.FileMetadata.ContentType,
        UploadedByName = x.UploadedByUser != null ? x.UploadedByUser.DisplayName : null,
        CreatedAtUtc = x.CreatedAt
    };
}
