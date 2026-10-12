using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Data.Models;
using web.Repositories.LabelCollections.Dtos;
using web.Repositories.LabelCollections.Interfaces;
using web.Repositories.LabelCollections.Mappers;

namespace web.Repositories.LabelCollections;

public class LabelCollectionRepository(WebDbContext dbContext) : ILabelCollectionRepository
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 500;

    public async Task<LabelCollectionPageDto> SearchAsync(SearchLabelCollectionsDto search, CancellationToken cancellationToken = default)
    {
        var page = search.Page < 1 ? 1 : search.Page;
        var pageSize = search.PageSize is < DefaultPageSize or > MaxPageSize ? DefaultPageSize : search.PageSize;

        var query = dbContext.LabelCollections.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var text = search.SearchText.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(text) || c.Items.Any(i => i.ElementsJson.ToLower().Contains(text)));
        }

        return new LabelCollectionPageDto
        {
            TotalCount = await query.CountAsync(cancellationToken),
            Items = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(LabelCollectionMapper.ToListItemDtoExpression)
                .ToListAsync(cancellationToken),
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.LabelCollections.AnyAsync(c => c.Id == id, cancellationToken);

    public async Task<SaveLabelCollectionResultDto> CreateAsync(CreateLabelCollectionDto dto, CancellationToken cancellationToken = default)
    {
        var name = dto.Name.Trim();
        if (name.Length == 0)
            return SaveLabelCollectionResultDto.Invalid("Navn skal udfyldes.");

        var collection = dto.ToEntity(name);
        dbContext.LabelCollections.Add(collection);
        await dbContext.SaveChangesAsync(cancellationToken);
        return SaveLabelCollectionResultDto.Saved(collection.Id, name);
    }

    public async Task<List<int>?> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var collection = await dbContext.LabelCollections
            .Include(c => c.Media)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (collection is null)
            return null;

        var fileIds = collection.Media.Select(m => m.FileMetadataId).ToList();
        // Labels and image rows go with the cascade delete.
        dbContext.LabelCollections.Remove(collection);
        await dbContext.SaveChangesAsync(cancellationToken);
        return fileIds;
    }

    public async Task<LabelCollectionDesignerDto?> GetDesignerAsync(int id, CancellationToken cancellationToken = default)
    {
        var collection = await dbContext.LabelCollections.AsNoTracking()
            .Include(c => c.Items)
            .Include(c => c.Media).ThenInclude(m => m.FileMetadata)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (collection is null)
            return null;

        return new LabelCollectionDesignerDto
        {
            Id = collection.Id,
            Design = collection.ToDesignDto(),
            Media = collection.Media
                .Where(m => !m.FileMetadata.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => m.ToDto())
                .ToList()
        };
    }

    public async Task<SaveLabelCollectionResultDto> SaveDesignAsync(int id, LabelCollectionDesignDto design, CancellationToken cancellationToken = default)
    {
        var collection = await dbContext.LabelCollections
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (collection is null)
            return SaveLabelCollectionResultDto.NotFound();

        var mediaIds = (await dbContext.LabelCollectionMedia
            .Where(m => m.LabelCollectionId == id)
            .Select(m => m.PublicId)
            .ToListAsync(cancellationToken)).ToHashSet();

        var (normalized, error) = LabelCollectionDesignNormalizer.Normalize(design, mediaIds);
        if (normalized is null)
            return SaveLabelCollectionResultDto.Invalid(error!);

        collection.Name = normalized.Name;
        collection.Across = normalized.Across;
        collection.Down = normalized.Down;
        collection.Landscape = normalized.Landscape;
        dbContext.Entry(collection).State = EntityState.Modified;

        // The labels have no ids anyone refers to, so the saved set simply replaces the old one.
        dbContext.LabelCollectionItems.RemoveRange(collection.Items);
        for (var n = 0; n < normalized.Labels.Count; n++)
            collection.Items.Add(normalized.Labels[n].ToEntity(n));

        await dbContext.SaveChangesAsync(cancellationToken);
        return SaveLabelCollectionResultDto.Saved(collection.Id, normalized.Name);
    }

    public async Task<LabelMediaDto?> AddMediaAsync(int id, int fileId, CancellationToken cancellationToken = default)
    {
        if (!await ExistsAsync(id, cancellationToken))
            return null;

        var media = new LabelCollectionMedia { LabelCollectionId = id, FileMetadataId = fileId };
        dbContext.LabelCollectionMedia.Add(media);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(media).Reference(m => m.FileMetadata).LoadAsync(cancellationToken);
        return media.ToDto();
    }

    public async Task<RemoveLabelCollectionMediaResultDto> RemoveMediaAsync(int id, Guid mediaId, CancellationToken cancellationToken = default)
    {
        var media = await dbContext.LabelCollectionMedia
            .FirstOrDefaultAsync(m => m.LabelCollectionId == id && m.PublicId == mediaId, cancellationToken);
        if (media is null)
            return new RemoveLabelCollectionMediaResultDto { Status = RemoveLabelCollectionMediaStatus.NotFound };

        var key = mediaId.ToString();
        if (await dbContext.LabelCollectionItems.AnyAsync(i => i.LabelCollectionId == id && i.ElementsJson.Contains(key), cancellationToken))
            return new RemoveLabelCollectionMediaResultDto { Status = RemoveLabelCollectionMediaStatus.InUse };

        dbContext.LabelCollectionMedia.Remove(media);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new RemoveLabelCollectionMediaResultDto { Status = RemoveLabelCollectionMediaStatus.Removed, FileId = media.FileMetadataId };
    }

    public Task<int?> GetMediaFileIdAsync(Guid mediaId, CancellationToken cancellationToken = default) =>
        dbContext.LabelCollectionMedia.AsNoTracking()
            .Where(m => m.PublicId == mediaId)
            .Select(m => (int?)m.FileMetadataId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<LabelCollectionPrintDto?> GetPrintAsync(int id, CancellationToken cancellationToken = default)
    {
        var collection = await dbContext.LabelCollections.AsNoTracking()
            .Include(c => c.Items)
            .Include(c => c.Media).ThenInclude(m => m.FileMetadata)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (collection is null)
            return null;

        return new LabelCollectionPrintDto
        {
            Name = collection.Name,
            Design = collection.ToDesignDto(),
            MediaFileIds = collection.Media
                .Where(m => !m.FileMetadata.IsDeleted)
                .ToDictionary(m => m.PublicId, m => m.FileMetadataId)
        };
    }
}
