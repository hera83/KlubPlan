using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Repositories.InfoScreens.Dtos;
using web.Repositories.InfoScreens.Interfaces;
using web.ViewModels;

namespace web.Repositories.InfoScreens
{
    public class InfoScreenService : IInfoScreenService
    {
        /// <summary>camelCase both ways — the designer and the player read/write the same JSON.</summary>
        public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private static readonly Regex HexColor = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);
        private static readonly Regex ElementId = new("^[A-Za-z0-9_-]{1,40}$", RegexOptions.Compiled);

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<InfoScreenService> _logger;

        public InfoScreenService(ApplicationDbContext context, IWebHostEnvironment env, IConfiguration config, ILogger<InfoScreenService> logger)
        {
            _context = context;
            _env = env;
            _config = config;
            _logger = logger;
        }

        // ─── Tabel ───────────────────────────────────────────────────────────────

        public async Task<InfoScreenFilterViewModel> GetScreensAsync(InfoScreenFilterViewModel filter, CancellationToken ct = default)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize is < 10 or > 500 ? 10 : filter.PageSize;

            var query = _context.InfoScreens.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var search = filter.SearchText.Trim().ToLower();
                query = query.Where(s => s.Title.ToLower().Contains(search));
            }
            if (filter.Status == "active")
                query = query.Where(s => s.IsActive);
            else if (filter.Status == "inactive")
                query = query.Where(s => !s.IsActive);

            filter.TotalCount = await query.CountAsync(ct);
            filter.Screens = await query
                .OrderBy(s => s.Title)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(s => new InfoScreenListItemViewModel
                {
                    Id = s.Id,
                    PublicId = s.PublicId,
                    Title = s.Title,
                    IsActive = s.IsActive,
                    SlideCount = s.Slides.Count,
                    VisibleSlideCount = s.Slides.Count(x => !x.IsHidden),
                    TotalSeconds = s.Slides.Where(x => !x.IsHidden).Sum(x => (int?)x.DurationSeconds) ?? 0,
                    ChangedAtUtc = s.UpdatedAtUtc ?? s.CreatedAtUtc
                })
                .ToListAsync(ct);

            return filter;
        }

        public async Task<InfoScreenActionResultDto> CreateAsync(InfoScreenCreateViewModel input, CancellationToken ct = default)
        {
            var title = input.Title.Trim();
            if (title.Length == 0)
                return InfoScreenActionResultDto.Fail("Titel skal udfyldes.");

            var now = DateTime.UtcNow;
            var screen = new InfoScreen
            {
                Title = title,
                IsActive = input.IsActive,
                AspectRatio = InfoScreenRules.DefaultAspectRatio,
                Transition = InfoScreenRules.DefaultTransition,
                CreatedAtUtc = now
            };
            // A starter slide with the title, so the designer opens on something to edit rather than an empty canvas.
            screen.Slides.Add(new InfoScreenSlide
            {
                Order = 0,
                DurationSeconds = InfoScreenRules.DefaultDurationSeconds,
                Background = InfoScreenRules.DefaultBackground,
                ElementsJson = JsonSerializer.Serialize(new[]
                {
                    new InfoScreenElementDto
                    {
                        Id = "e1", Type = InfoScreenRules.ElementText, X = 10, Y = 35, W = 80, H = 30,
                        Text = title, FontSize = 12, FontFamily = "sans", Bold = true, Italic = false,
                        Align = "center", VAlign = "middle", Color = "#ffffff", Background = ""
                    }
                }, JsonOptions),
                CreatedAtUtc = now
            });

            _context.InfoScreens.Add(screen);
            await _context.SaveChangesAsync(ct);
            return InfoScreenActionResultDto.Ok($"Infoskærmen \"{title}\" er oprettet.", screen.Id);
        }

        public async Task<InfoScreenActionResultDto> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
        {
            var screen = await _context.InfoScreens.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (screen is null)
                return InfoScreenActionResultDto.Fail("Infoskærmen blev ikke fundet.");

            screen.IsActive = isActive;
            screen.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return InfoScreenActionResultDto.Ok(isActive
                ? $"Infoskærmen \"{screen.Title}\" er aktiveret."
                : $"Infoskærmen \"{screen.Title}\" er deaktiveret.", screen.Id);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var screen = await _context.InfoScreens
                .Include(s => s.Media).ThenInclude(m => m.FileMetadata)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
            if (screen is null)
                return false;

            var files = screen.Media.Select(m => m.FileMetadata).ToList();
            _context.FileMetadata.RemoveRange(files);
            // Slides and the media rows go with the cascade delete.
            _context.InfoScreens.Remove(screen);
            await _context.SaveChangesAsync(ct);

            foreach (var file in files)
                DeletePhysicalFile(file.StoredPath);
            _logger.LogInformation("Info screen {ScreenId} deleted with {FileCount} media files", id, files.Count);
            return true;
        }

        // ─── Designer ────────────────────────────────────────────────────────────

        public async Task<InfoScreenDesignerViewModel?> GetDesignerAsync(int id, CancellationToken ct = default)
        {
            var screen = await _context.InfoScreens.AsNoTracking()
                .Include(s => s.Slides)
                .Include(s => s.Media).ThenInclude(m => m.FileMetadata)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
            if (screen is null)
                return null;

            return new InfoScreenDesignerViewModel
            {
                Id = screen.Id,
                PublicId = screen.PublicId,
                Design = new InfoScreenDesignDto
                {
                    Title = screen.Title,
                    IsActive = screen.IsActive,
                    AspectRatio = screen.AspectRatio,
                    Transition = screen.Transition,
                    Slides = screen.Slides.OrderBy(s => s.Order).Select(ToSlideDto).ToList()
                },
                Media = screen.Media
                    .Where(m => !m.FileMetadata.IsDeleted)
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Select(ToMediaDto)
                    .ToList()
            };
        }

        public async Task<InfoScreenActionResultDto> SaveDesignAsync(int id, InfoScreenDesignDto design, CancellationToken ct = default)
        {
            var screen = await _context.InfoScreens
                .Include(s => s.Slides)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
            if (screen is null)
                return InfoScreenActionResultDto.Fail("Infoskærmen blev ikke fundet.");

            var title = (design.Title ?? string.Empty).Trim();
            if (title.Length == 0)
                return InfoScreenActionResultDto.Fail("Titel skal udfyldes.");
            if (title.Length > InfoScreenRules.MaxTitleLength)
                return InfoScreenActionResultDto.Fail($"Titlen må højst være {InfoScreenRules.MaxTitleLength} tegn.");
            if (design.Slides.Count > InfoScreenRules.MaxSlides)
                return InfoScreenActionResultDto.Fail($"En infoskærm kan højst have {InfoScreenRules.MaxSlides} sider.");

            var mediaIds = (await _context.InfoScreenMedia
                .Where(m => m.InfoScreenId == id)
                .Select(m => m.PublicId)
                .ToListAsync(ct)).ToHashSet();

            var slides = new List<InfoScreenSlide>();
            var now = DateTime.UtcNow;
            for (var n = 0; n < design.Slides.Count; n++)
            {
                var input = design.Slides[n];
                if (input.Elements.Count > InfoScreenRules.MaxElementsPerSlide)
                    return InfoScreenActionResultDto.Fail($"Side {n + 1} har for mange elementer (højst {InfoScreenRules.MaxElementsPerSlide}).");

                var elements = new List<InfoScreenElementDto>();
                foreach (var element in input.Elements)
                {
                    var normalized = NormalizeElement(element, mediaIds, out var error);
                    if (error is not null)
                        return InfoScreenActionResultDto.Fail($"Side {n + 1}: {error}");
                    if (normalized is not null)
                        elements.Add(normalized);
                }

                slides.Add(new InfoScreenSlide
                {
                    Order = n,
                    DurationSeconds = Math.Clamp(input.DurationSeconds, InfoScreenRules.MinDurationSeconds, InfoScreenRules.MaxDurationSeconds),
                    Background = ColorOr(input.Background, InfoScreenRules.DefaultBackground),
                    IsHidden = input.IsHidden,
                    ElementsJson = JsonSerializer.Serialize(elements, JsonOptions),
                    CreatedAtUtc = now
                });
            }

            screen.Title = title;
            screen.IsActive = design.IsActive;
            screen.AspectRatio = InfoScreenRules.AspectRatios.Any(a => a.Key == design.AspectRatio) ? design.AspectRatio : InfoScreenRules.DefaultAspectRatio;
            screen.Transition = InfoScreenRules.Transitions.Any(t => t.Key == design.Transition) ? design.Transition : InfoScreenRules.DefaultTransition;
            screen.UpdatedAtUtc = now;

            // The slides have no ids anyone refers to, so the saved set simply replaces the old one.
            _context.InfoScreenSlides.RemoveRange(screen.Slides);
            foreach (var slide in slides)
                screen.Slides.Add(slide);

            await _context.SaveChangesAsync(ct);
            return InfoScreenActionResultDto.Ok($"Infoskærmen \"{title}\" er gemt.", screen.Id);
        }

        /// <summary>Only the fields for the element's type are kept; numbers are clamped and colors/choices checked. Null = drop it.</summary>
        private static InfoScreenElementDto? NormalizeElement(InfoScreenElementDto e, IReadOnlySet<Guid> mediaIds, out string? error)
        {
            error = null;
            if (!InfoScreenRules.ElementTypes.Contains(e.Type))
                return null;

            var result = new InfoScreenElementDto
            {
                Id = e.Id is not null && ElementId.IsMatch(e.Id) ? e.Id : Guid.NewGuid().ToString("N")[..12],
                Type = e.Type,
                X = Round(Math.Clamp(e.X, -50, 100)),
                Y = Round(Math.Clamp(e.Y, -50, 100)),
                W = Round(Math.Clamp(e.W, 0.5, 200)),
                H = Round(Math.Clamp(e.H, 0.5, 200))
            };

            switch (e.Type)
            {
                case InfoScreenRules.ElementText:
                case InfoScreenRules.ElementClock:
                    if (e.Type == InfoScreenRules.ElementText)
                    {
                        var text = (e.Text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
                        if (text.Length > InfoScreenRules.MaxTextLength)
                        {
                            error = $"En tekst må højst være {InfoScreenRules.MaxTextLength} tegn.";
                            return null;
                        }
                        result.Text = text;
                    }
                    else
                    {
                        result.Format = Choice(e.Format, InfoScreenRules.ClockFormats, "time");
                    }
                    result.FontSize = Round(Math.Clamp(e.FontSize ?? 8, InfoScreenRules.MinFontSize, InfoScreenRules.MaxFontSize));
                    result.FontFamily = Choice(e.FontFamily, InfoScreenRules.FontFamilies, "sans");
                    result.Bold = e.Bold ?? false;
                    result.Italic = e.Italic ?? false;
                    result.Align = Choice(e.Align, InfoScreenRules.Aligns, "center");
                    result.VAlign = Choice(e.VAlign, InfoScreenRules.VAligns, "middle");
                    result.Color = ColorOr(e.Color, "#ffffff");
                    result.Background = ColorOr(e.Background, string.Empty);
                    break;

                case InfoScreenRules.ElementImage:
                case InfoScreenRules.ElementVideo:
                    // A file removed from the library leaves an empty box rather than a broken element.
                    result.MediaId = e.MediaId is Guid mediaId && mediaIds.Contains(mediaId) ? mediaId : null;
                    result.Fit = Choice(e.Fit, InfoScreenRules.Fits, "contain");
                    break;

                case InfoScreenRules.ElementQr:
                    var qrText = (e.Text ?? string.Empty).Trim();
                    if (qrText.Length > InfoScreenRules.MaxQrTextLength)
                    {
                        error = $"Teksten i en QR-kode må højst være {InfoScreenRules.MaxQrTextLength} tegn.";
                        return null;
                    }
                    result.Text = qrText;
                    result.Color = ColorOr(e.Color, "#000000");
                    break;
            }
            return result;
        }

        private static double Round(double value) => Math.Round(value, 2);

        private static string Choice(string? value, IReadOnlySet<string> allowed, string fallback)
            => value is not null && allowed.Contains(value) ? value : fallback;

        private static string ColorOr(string? value, string fallback)
            => value is not null && HexColor.IsMatch(value) ? value.ToLowerInvariant() : fallback;

        private static InfoScreenSlideDto ToSlideDto(InfoScreenSlide slide) => new()
        {
            DurationSeconds = slide.DurationSeconds,
            Background = slide.Background,
            IsHidden = slide.IsHidden,
            Elements = DeserializeElements(slide.ElementsJson)
        };

        private static List<InfoScreenElementDto> DeserializeElements(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<InfoScreenElementDto>>(json, JsonOptions) ?? new();
            }
            catch (JsonException)
            {
                return new();
            }
        }

        private static InfoScreenMediaDto ToMediaDto(InfoScreenMedia media) => new()
        {
            Id = media.PublicId,
            Name = media.FileMetadata.OriginalFileName,
            Kind = media.Kind,
            SizeBytes = media.FileMetadata.FileSizeBytes
        };

        // ─── Medier ──────────────────────────────────────────────────────────────

        public async Task<InfoScreenActionResultDto> UploadMediaAsync(int id, Stream content, string fileName, long length, string? ownerId, CancellationToken ct = default)
        {
            if (!await _context.InfoScreens.AnyAsync(s => s.Id == id, ct))
                return InfoScreenActionResultDto.Fail("Infoskærmen blev ikke fundet.");

            // The content type comes from our own list, never from the browser.
            var ext = Path.GetExtension(fileName);
            string kind, contentType;
            long maxBytes;
            if (InfoScreenRules.ImageTypes.TryGetValue(ext, out var imageType))
            {
                (kind, contentType, maxBytes) = (InfoScreenRules.MediaKindImage, imageType, InfoScreenRules.MaxImageBytes);
            }
            else if (InfoScreenRules.VideoTypes.TryGetValue(ext, out var videoType))
            {
                (kind, contentType, maxBytes) = (InfoScreenRules.MediaKindVideo, videoType, InfoScreenRules.MaxVideoBytes);
            }
            else
            {
                return InfoScreenActionResultDto.Fail("Filtypen understøttes ikke. Brug billeder (PNG, JPG, GIF, WEBP) eller video (MP4, WEBM).");
            }

            if (length <= 0)
                return InfoScreenActionResultDto.Fail("Filen er tom.");
            if (length > maxBytes)
                return InfoScreenActionResultDto.Fail($"Filen er for stor. {(kind == InfoScreenRules.MediaKindImage ? "Billeder" : "Videoer")} må højst være {maxBytes / (1024 * 1024)} MB.");

            var relativeDir = Path.Combine(FilesRoot, FileCategories.InfoScreens, id.ToString(CultureInfo.InvariantCulture));
            Directory.CreateDirectory(Path.Combine(_env.ContentRootPath, relativeDir));
            var storedFileName = $"{Guid.NewGuid()}{ext.ToLowerInvariant()}";
            var storedPath = Path.Combine(relativeDir, storedFileName);
            var fullPath = Path.Combine(_env.ContentRootPath, storedPath);

            await using (var target = File.Create(fullPath))
            {
                await content.CopyToAsync(target, ct);
            }

            var metadata = new FileMetadata
            {
                OriginalFileName = Path.GetFileName(fileName),
                StoredFileName = storedFileName,
                StoredPath = storedPath,
                ContentType = contentType,
                FileSizeBytes = new FileInfo(fullPath).Length,
                OwnerId = ownerId,
                Category = FileCategories.InfoScreens,
                CreatedAtUtc = DateTime.UtcNow
            };
            var media = new InfoScreenMedia { InfoScreenId = id, Kind = kind, FileMetadata = metadata, CreatedAtUtc = DateTime.UtcNow };
            _context.InfoScreenMedia.Add(media);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Info screen {ScreenId} got {Kind} {FileName}", id, kind, storedFileName);
            var result = InfoScreenActionResultDto.Ok($"\"{metadata.OriginalFileName}\" er uploadet.", id);
            result.Media = ToMediaDto(media);
            return result;
        }

        public async Task<InfoScreenActionResultDto> DeleteMediaAsync(int id, Guid mediaId, CancellationToken ct = default)
        {
            var media = await _context.InfoScreenMedia
                .Include(m => m.FileMetadata)
                .FirstOrDefaultAsync(m => m.InfoScreenId == id && m.PublicId == mediaId, ct);
            if (media is null)
                return InfoScreenActionResultDto.Fail("Filen blev ikke fundet.");

            var key = mediaId.ToString();
            if (await _context.InfoScreenSlides.AnyAsync(s => s.InfoScreenId == id && s.ElementsJson.Contains(key), ct))
                return InfoScreenActionResultDto.Fail("Filen bruges på en gemt side. Fjern den fra siden og gem først.");

            var storedPath = media.FileMetadata.StoredPath;
            // The media row goes with the cascade delete.
            _context.FileMetadata.Remove(media.FileMetadata);
            await _context.SaveChangesAsync(ct);
            DeletePhysicalFile(storedPath);
            return InfoScreenActionResultDto.Ok("Filen er slettet.", id);
        }

        // ─── Offentlig visning ───────────────────────────────────────────────────

        public async Task<InfoScreenPlayerDto> GetPlayerDataAsync(Guid publicId, bool preview, CancellationToken ct = default)
        {
            var screen = await _context.InfoScreens.AsNoTracking()
                .Include(s => s.Slides)
                .FirstOrDefaultAsync(s => s.PublicId == publicId, ct);
            if (screen is null)
                return new InfoScreenPlayerDto { Status = InfoScreenPlayerDto.StatusNotFound };

            var dto = new InfoScreenPlayerDto
            {
                Title = screen.Title,
                AspectRatio = screen.AspectRatio,
                Transition = screen.Transition,
                Version = (screen.UpdatedAtUtc ?? screen.CreatedAtUtc).Ticks
            };
            if (!screen.IsActive && !preview)
            {
                dto.Status = InfoScreenPlayerDto.StatusInactive;
                return dto;
            }

            dto.Status = InfoScreenPlayerDto.StatusOk;
            dto.Slides = screen.Slides.Where(s => !s.IsHidden).OrderBy(s => s.Order).Select(ToSlideDto).ToList();
            return dto;
        }

        public async Task<string?> GetTitleAsync(Guid publicId, CancellationToken ct = default)
            => await _context.InfoScreens.Where(s => s.PublicId == publicId).Select(s => s.Title).FirstOrDefaultAsync(ct);

        public async Task<(string FullPath, string ContentType)?> GetMediaFileAsync(Guid mediaId, CancellationToken ct = default)
        {
            var file = await _context.InfoScreenMedia.AsNoTracking()
                .Where(m => m.PublicId == mediaId && !m.FileMetadata.IsDeleted)
                .Select(m => new { m.FileMetadata.StoredPath, m.FileMetadata.ContentType })
                .FirstOrDefaultAsync(ct);
            if (file is null)
                return null;

            var fullPath = Path.Combine(_env.ContentRootPath, file.StoredPath);
            return File.Exists(fullPath) ? (fullPath, file.ContentType) : null;
        }

        private string FilesRoot => _config["AppSettings:FilesPath"] ?? "App_files";

        private void DeletePhysicalFile(string storedPath)
        {
            try
            {
                var fullPath = Path.Combine(_env.ContentRootPath, storedPath);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Info screen media file {StoredPath} could not be deleted", storedPath);
            }
        }
    }
}
