using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Extensions;
using web.Repositories.Files.Dtos;
using web.Repositories.Files.Interfaces;
using web.Repositories.Files.Mappers;

namespace web.Repositories.Files;

public class FileRepository(WebDbContext dbContext, IWebHostEnvironment environment, ILogger<FileRepository> logger) : IFileRepository
{
    private const int MaxExtensionLength = 20;

    public async Task<StoredFileDto> CreateAsync(CreateStoredFileDto dto, Stream content, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(dto.OriginalFileName).ToLowerInvariant();
        if (extension.Length > MaxExtensionLength)
        {
            extension = string.Empty;
        }

        var storedFileName = Guid.NewGuid().ToString("N") + extension;
        var relativeDirectory = string.IsNullOrEmpty(dto.SubFolder) ? dto.Category : $"{dto.Category}/{dto.SubFolder}";
        var relativePath = $"{relativeDirectory}/{storedFileName}";
        var fullPath = AppFilesHelper.GetFullPath(environment.ContentRootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using (var target = File.Create(fullPath))
        {
            await content.CopyToAsync(target, cancellationToken);
        }

        var entity = dto.ToEntity(storedFileName, relativePath, new FileInfo(fullPath).Length);
        dbContext.FileMetadata.Add(entity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            TryDeletePhysicalFile(fullPath);
            throw;
        }

        return entity.ToDto(environment.ContentRootPath);
    }

    public async Task<StoredFileDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.FileMetadata.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var dto = entity.ToDto(environment.ContentRootPath);
        return File.Exists(dto.FullPath) ? dto : null;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.FileMetadata.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        dbContext.FileMetadata.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        TryDeletePhysicalFile(AppFilesHelper.GetFullPath(environment.ContentRootPath, entity.StoredPath));
        return true;
    }

    public Task DeleteEmptyFolderAsync(string category, string subFolder, CancellationToken cancellationToken = default)
    {
        var fullPath = AppFilesHelper.GetFullPath(environment.ContentRootPath, $"{category}/{subFolder}");
        try
        {
            if (Directory.Exists(fullPath) && !Directory.EnumerateFileSystemEntries(fullPath).Any())
                Directory.Delete(fullPath);
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Could not remove folder {FullPath}", fullPath);
        }

        return Task.CompletedTask;
    }

    private void TryDeletePhysicalFile(string fullPath)
    {
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Could not delete physical file {FullPath}", fullPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Could not delete physical file {FullPath}", fullPath);
        }
    }
}
