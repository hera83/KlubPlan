using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Data.Models;
using web.Repositories.InfoScreens.Dtos;
using web.Repositories.InfoScreens.Interfaces;
using web.Repositories.InfoScreens.Mappers;

namespace web.Repositories.InfoScreens;

public class InfoScreenRepository(WebDbContext dbContext) : IInfoScreenRepository
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 500;

    public async Task<InfoScreenPageDto> SearchAsync(SearchInfoScreensDto search, CancellationToken cancellationToken = default)
    {
        var page = search.Page < 1 ? 1 : search.Page;
        var pageSize = search.PageSize is < DefaultPageSize or > MaxPageSize ? DefaultPageSize : search.PageSize;

        var query = dbContext.InfoScreens.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var text = search.SearchText.Trim().ToLower();
            query = query.Where(s => s.Title.ToLower().Contains(text));
        }
        if (search.Status == "active")
            query = query.Where(s => s.IsActive);
        else if (search.Status == "inactive")
            query = query.Where(s => !s.IsActive);

        return new InfoScreenPageDto
        {
            TotalCount = await query.CountAsync(cancellationToken),
            Items = await query
                .OrderBy(s => s.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(InfoScreenMapper.ToListItemDtoExpression)
                .ToListAsync(cancellationToken),
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.InfoScreens.AnyAsync(s => s.Id == id, cancellationToken);

    public async Task<SaveInfoScreenResultDto> CreateAsync(CreateInfoScreenDto dto, CancellationToken cancellationToken = default)
    {
        var title = dto.Title.Trim();
        if (title.Length == 0)
            return SaveInfoScreenResultDto.Invalid("Titel skal udfyldes.");

        var screen = dto.ToEntity(title);
        dbContext.InfoScreens.Add(screen);
        await dbContext.SaveChangesAsync(cancellationToken);
        return SaveInfoScreenResultDto.Saved(screen.Id, title);
    }

    public async Task<SaveInfoScreenResultDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var screen = await dbContext.InfoScreens.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (screen is null)
            return SaveInfoScreenResultDto.NotFound();

        screen.IsActive = isActive;
        // Touch the screen even when IsActive didn't change, so running screens pick up a new Version.
        dbContext.Entry(screen).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return SaveInfoScreenResultDto.Saved(screen.Id, screen.Title);
    }

    public async Task<List<int>?> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var screen = await dbContext.InfoScreens
            .Include(s => s.Media)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (screen is null)
            return null;

        var fileIds = screen.Media.Select(m => m.FileMetadataId).ToList();
        // Slides and media rows go with the cascade delete.
        dbContext.InfoScreens.Remove(screen);
        await dbContext.SaveChangesAsync(cancellationToken);
        return fileIds;
    }

    public async Task<InfoScreenDesignerDto?> GetDesignerAsync(int id, CancellationToken cancellationToken = default)
    {
        var screen = await dbContext.InfoScreens.AsNoTracking()
            .Include(s => s.Slides)
            .Include(s => s.Media).ThenInclude(m => m.FileMetadata)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (screen is null)
            return null;

        return new InfoScreenDesignerDto
        {
            Id = screen.Id,
            PublicId = screen.PublicId,
            Design = screen.ToDesignDto(),
            Media = screen.Media
                .Where(m => !m.FileMetadata.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => m.ToDto())
                .ToList()
        };
    }

    public async Task<SaveInfoScreenResultDto> SaveDesignAsync(int id, InfoScreenDesignDto design, CancellationToken cancellationToken = default)
    {
        var screen = await dbContext.InfoScreens
            .Include(s => s.Slides)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (screen is null)
            return SaveInfoScreenResultDto.NotFound();

        var mediaIds = (await dbContext.InfoScreenMedia
            .Where(m => m.InfoScreenId == id)
            .Select(m => m.PublicId)
            .ToListAsync(cancellationToken)).ToHashSet();

        var (normalized, error) = InfoScreenDesignNormalizer.Normalize(design, mediaIds);
        if (normalized is null)
            return SaveInfoScreenResultDto.Invalid(error!);

        screen.Title = normalized.Title;
        screen.IsActive = normalized.IsActive;
        screen.AspectRatio = normalized.AspectRatio;
        screen.Transition = normalized.Transition;
        dbContext.Entry(screen).State = EntityState.Modified;

        // The slides have no ids anyone refers to, so the saved set simply replaces the old one.
        dbContext.InfoScreenSlides.RemoveRange(screen.Slides);
        for (var n = 0; n < normalized.Slides.Count; n++)
            screen.Slides.Add(normalized.Slides[n].ToEntity(n));

        await dbContext.SaveChangesAsync(cancellationToken);
        return SaveInfoScreenResultDto.Saved(screen.Id, normalized.Title);
    }

    public async Task<InfoScreenMediaDto?> AddMediaAsync(int id, int fileId, string kind, CancellationToken cancellationToken = default)
    {
        if (!await ExistsAsync(id, cancellationToken))
            return null;

        var media = new InfoScreenMedia { InfoScreenId = id, Kind = kind, FileMetadataId = fileId };
        dbContext.InfoScreenMedia.Add(media);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(media).Reference(m => m.FileMetadata).LoadAsync(cancellationToken);
        return media.ToDto();
    }

    public async Task<RemoveInfoScreenMediaResultDto> RemoveMediaAsync(int id, Guid mediaId, CancellationToken cancellationToken = default)
    {
        var media = await dbContext.InfoScreenMedia
            .FirstOrDefaultAsync(m => m.InfoScreenId == id && m.PublicId == mediaId, cancellationToken);
        if (media is null)
            return new RemoveInfoScreenMediaResultDto { Status = RemoveInfoScreenMediaStatus.NotFound };

        var key = mediaId.ToString();
        if (await dbContext.InfoScreenSlides.AnyAsync(s => s.InfoScreenId == id && s.ElementsJson.Contains(key), cancellationToken))
            return new RemoveInfoScreenMediaResultDto { Status = RemoveInfoScreenMediaStatus.InUse };

        dbContext.InfoScreenMedia.Remove(media);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new RemoveInfoScreenMediaResultDto { Status = RemoveInfoScreenMediaStatus.Removed, FileId = media.FileMetadataId };
    }

    public Task<int?> GetMediaFileIdAsync(Guid mediaId, CancellationToken cancellationToken = default) =>
        dbContext.InfoScreenMedia.AsNoTracking()
            .Where(m => m.PublicId == mediaId)
            .Select(m => (int?)m.FileMetadataId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<InfoScreenPlayerDto> GetPlayerAsync(Guid publicId, bool preview, CancellationToken cancellationToken = default)
    {
        var screen = await dbContext.InfoScreens.AsNoTracking()
            .Include(s => s.Slides)
            .FirstOrDefaultAsync(s => s.PublicId == publicId, cancellationToken);
        if (screen is null)
            return new InfoScreenPlayerDto { Status = InfoScreenPlayerDto.StatusNotFound };

        var dto = new InfoScreenPlayerDto
        {
            Title = screen.Title,
            AspectRatio = screen.AspectRatio,
            Transition = screen.Transition,
            Version = screen.UpdatedAt.Ticks
        };
        if (!screen.IsActive && !preview)
        {
            dto.Status = InfoScreenPlayerDto.StatusInactive;
            return dto;
        }

        dto.Status = InfoScreenPlayerDto.StatusOk;
        dto.Slides = screen.Slides.Where(s => !s.IsHidden).OrderBy(s => s.Order).Select(s => s.ToDto()).ToList();
        return dto;
    }

    public Task<string?> GetTitleAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        dbContext.InfoScreens.AsNoTracking()
            .Where(s => s.PublicId == publicId)
            .Select(s => s.Title)
            .FirstOrDefaultAsync(cancellationToken);
}
