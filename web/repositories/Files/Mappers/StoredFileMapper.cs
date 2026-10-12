using web.Data.Models;
using web.Extensions;
using web.Repositories.Files.Dtos;

namespace web.Repositories.Files.Mappers;

public static class StoredFileMapper
{
    public static StoredFileDto ToDto(this FileMetadata entity, string contentRootPath) => new()
    {
        Id = entity.Id,
        OriginalFileName = entity.OriginalFileName,
        ContentType = entity.ContentType,
        FileSizeBytes = entity.FileSizeBytes,
        Category = entity.Category,
        OwnerId = entity.OwnerId,
        FullPath = AppFilesHelper.GetFullPath(contentRootPath, entity.StoredPath),
        CreatedAt = entity.CreatedAt
    };

    public static FileMetadata ToEntity(this CreateStoredFileDto dto, string storedFileName, string relativePath, long sizeBytes) => new()
    {
        OriginalFileName = dto.OriginalFileName,
        StoredFileName = storedFileName,
        StoredPath = relativePath,
        ContentType = dto.ContentType,
        FileSizeBytes = sizeBytes,
        OwnerId = dto.OwnerId,
        Category = dto.Category
    };
}
