using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Infrastructure;
using web.Infrastructure.Labels;
using web.Repositories.LabelCollections.Dtos;
using web.Repositories.LabelCollections.Interfaces;
using web.ViewModels;

namespace web.Repositories.LabelCollections
{
    public class LabelCollectionService : ILabelCollectionService
    {
        /// <summary>camelCase both ways — the designer reads and writes the same JSON. The default encoder escapes &lt;, &gt; and &amp;, so it is safe inside a &lt;script&gt; block.</summary>
        public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        /// <summary>
        /// How ElementsJson is stored: unused fields left out, and æ/ø/å kept as they are (not æ),
        /// so the table's search can find a label's text.
        /// </summary>
        private static readonly JsonSerializerOptions StorageJsonOptions = new(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly Regex HexColor = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);
        private static readonly Regex ElementId = new("^[A-Za-z0-9_-]{1,40}$", RegexOptions.Compiled);

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<LabelCollectionService> _logger;

        public LabelCollectionService(ApplicationDbContext context, IWebHostEnvironment env, IConfiguration config, ILogger<LabelCollectionService> logger)
        {
            _context = context;
            _env = env;
            _config = config;
            _logger = logger;
        }

        // ─── Tabel ───────────────────────────────────────────────────────────────

        public async Task<LabelCollectionFilterViewModel> GetCollectionsAsync(LabelCollectionFilterViewModel filter, CancellationToken ct = default)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize is < 10 or > 500 ? 10 : filter.PageSize;

            var query = _context.LabelCollections.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var search = filter.SearchText.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(search) || c.Items.Any(i => i.ElementsJson.ToLower().Contains(search)));
            }

            filter.TotalCount = await query.CountAsync(ct);
            filter.Collections = await query
                .OrderBy(c => c.Name)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => new LabelCollectionListItemViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    LabelCount = c.Items.Count,
                    Copies = c.Items.Sum(i => (int?)i.Quantity) ?? 0,
                    Across = c.Across,
                    Down = c.Down,
                    Landscape = c.Landscape,
                    ChangedAtUtc = c.UpdatedAtUtc ?? c.CreatedAtUtc
                })
                .ToListAsync(ct);

            return filter;
        }

        public async Task<LabelCollectionActionResultDto> CreateAsync(LabelCollectionCreateViewModel input, CancellationToken ct = default)
        {
            var name = input.Name.Trim();
            if (name.Length == 0)
                return LabelCollectionActionResultDto.Fail("Navn skal udfyldes.");

            var now = DateTime.UtcNow;
            var collection = new LabelCollection
            {
                Name = name,
                Across = Math.Clamp(input.Across, 1, LabelRules.MaxAcross),
                Down = Math.Clamp(input.Down, 1, LabelRules.MaxDown),
                Landscape = input.Landscape,
                CreatedAtUtc = now
            };
            // A starter label with the name, so the designer opens on something to edit rather than an empty label.
            collection.Items.Add(new LabelCollectionItem
            {
                Order = 0,
                Quantity = 1,
                Background = LabelDesignRules.DefaultBackground,
                ElementsJson = JsonSerializer.Serialize(new[]
                {
                    new LabelElementDto
                    {
                        Id = "e1", Type = LabelDesignRules.ElementText, X = 5, Y = 20, W = 90, H = 60,
                        Text = name, FontSize = 24, Bold = true, Italic = false,
                        Align = "center", VAlign = "middle", Color = LabelDesignRules.DefaultTextColor, Background = ""
                    }
                }, StorageJsonOptions),
                CreatedAtUtc = now
            });

            _context.LabelCollections.Add(collection);
            await _context.SaveChangesAsync(ct);
            return LabelCollectionActionResultDto.Ok($"Label-samlingen \"{name}\" er oprettet.", collection.Id);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var collection = await _context.LabelCollections
                .Include(c => c.Media).ThenInclude(m => m.FileMetadata)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
            if (collection is null)
                return false;

            var files = collection.Media.Select(m => m.FileMetadata).ToList();
            _context.FileMetadata.RemoveRange(files);
            // Labels and the media rows go with the cascade delete.
            _context.LabelCollections.Remove(collection);
            await _context.SaveChangesAsync(ct);

            foreach (var file in files)
                DeletePhysicalFile(file.StoredPath);
            _logger.LogInformation("Label collection {CollectionId} deleted with {FileCount} images", id, files.Count);
            return true;
        }

        // ─── Designer ────────────────────────────────────────────────────────────

        public async Task<LabelDesignerViewModel?> GetDesignerAsync(int id, CancellationToken ct = default)
        {
            var collection = await _context.LabelCollections.AsNoTracking()
                .Include(c => c.Items)
                .Include(c => c.Media).ThenInclude(m => m.FileMetadata)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
            if (collection is null)
                return null;

            return new LabelDesignerViewModel
            {
                Id = collection.Id,
                Design = ToDesignDto(collection),
                Media = collection.Media
                    .Where(m => !m.FileMetadata.IsDeleted)
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Select(ToMediaDto)
                    .ToList()
            };
        }

        public async Task<LabelCollectionActionResultDto> SaveDesignAsync(int id, LabelCollectionDesignDto design, CancellationToken ct = default)
        {
            var collection = await _context.LabelCollections
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
            if (collection is null)
                return LabelCollectionActionResultDto.Fail("Label-samlingen blev ikke fundet.");

            var name = (design.Name ?? string.Empty).Trim();
            if (name.Length == 0)
                return LabelCollectionActionResultDto.Fail("Navn skal udfyldes.");
            if (name.Length > LabelRules.MaxCollectionNameLength)
                return LabelCollectionActionResultDto.Fail($"Navnet må højst være {LabelRules.MaxCollectionNameLength} tegn.");
            if (design.Across is < 1 or > LabelRules.MaxAcross)
                return LabelCollectionActionResultDto.Fail($"Labels i bredden skal være mellem 1 og {LabelRules.MaxAcross}.");
            if (design.Down is < 1 or > LabelRules.MaxDown)
                return LabelCollectionActionResultDto.Fail($"Labels i højden skal være mellem 1 og {LabelRules.MaxDown}.");
            if (design.Labels.Count > LabelRules.MaxLabelsPerCollection)
                return LabelCollectionActionResultDto.Fail($"En samling kan højst have {LabelRules.MaxLabelsPerCollection} forskellige labels.");

            var mediaIds = (await _context.LabelCollectionMedia
                .Where(m => m.LabelCollectionId == id)
                .Select(m => m.PublicId)
                .ToListAsync(ct)).ToHashSet();

            var items = new List<LabelCollectionItem>();
            var now = DateTime.UtcNow;
            for (var n = 0; n < design.Labels.Count; n++)
            {
                var input = design.Labels[n];
                if (input.Quantity is < 1 or > LabelRules.MaxQuantity)
                    return LabelCollectionActionResultDto.Fail($"Label {n + 1}: antal skal være mellem 1 og {LabelRules.MaxQuantity}.");
                if (input.Elements.Count > LabelDesignRules.MaxElementsPerLabel)
                    return LabelCollectionActionResultDto.Fail($"Label {n + 1} har for mange elementer (højst {LabelDesignRules.MaxElementsPerLabel}).");

                var elements = new List<LabelElementDto>();
                foreach (var element in input.Elements)
                {
                    var normalized = NormalizeElement(element, mediaIds, out var error);
                    if (error is not null)
                        return LabelCollectionActionResultDto.Fail($"Label {n + 1}: {error}");
                    if (normalized is not null)
                        elements.Add(normalized);
                }

                items.Add(new LabelCollectionItem
                {
                    Order = n,
                    Quantity = input.Quantity,
                    Background = ColorOr(input.Background, LabelDesignRules.DefaultBackground),
                    ElementsJson = JsonSerializer.Serialize(elements, StorageJsonOptions),
                    CreatedAtUtc = now
                });
            }

            collection.Name = name;
            collection.Across = design.Across;
            collection.Down = design.Down;
            collection.Landscape = design.Landscape;
            collection.UpdatedAtUtc = now;

            // The labels have no ids anyone refers to, so the saved set simply replaces the old one.
            _context.LabelCollectionItems.RemoveRange(collection.Items);
            foreach (var item in items)
                collection.Items.Add(item);

            await _context.SaveChangesAsync(ct);
            return LabelCollectionActionResultDto.Ok($"Label-samlingen \"{name}\" er gemt.", collection.Id);
        }

        /// <summary>
        /// Only the fields for the element's type are kept; numbers are clamped and colors/choices checked.
        /// The box is kept inside the label — on a sheet, anything outside would print on the neighbour. Null = drop it.
        /// </summary>
        private static LabelElementDto? NormalizeElement(LabelElementDto e, IReadOnlySet<Guid> mediaIds, out string? error)
        {
            error = null;
            if (!LabelDesignRules.ElementTypes.Contains(e.Type))
                return null;

            var w = Round(Math.Clamp(e.W, 0.5, 100));
            var h = Round(Math.Clamp(e.H, 0.5, 100));
            var result = new LabelElementDto
            {
                Id = e.Id is not null && ElementId.IsMatch(e.Id) ? e.Id : Guid.NewGuid().ToString("N")[..12],
                Type = e.Type,
                X = Round(Math.Clamp(e.X, 0, 100 - w)),
                Y = Round(Math.Clamp(e.Y, 0, 100 - h)),
                W = w,
                H = h
            };

            switch (e.Type)
            {
                case LabelDesignRules.ElementText:
                case LabelDesignRules.ElementSerial:
                    if (e.Type == LabelDesignRules.ElementText)
                    {
                        var text = (e.Text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
                        if (text.Length > LabelDesignRules.MaxTextLength)
                        {
                            error = $"En tekst må højst være {LabelDesignRules.MaxTextLength} tegn.";
                            return null;
                        }
                        result.Text = text;
                    }
                    else
                    {
                        result.Start = Math.Clamp(e.Start ?? 1, 0, LabelDesignRules.MaxSerialStart);
                        result.Digits = Math.Clamp(e.Digits ?? 0, 0, LabelDesignRules.MaxSerialDigits);
                        result.Prefix = Affix(e.Prefix);
                        result.Suffix = Affix(e.Suffix);
                    }
                    result.FontSize = Round(Math.Clamp(e.FontSize ?? LabelDesignRules.DefaultFontSize, LabelDesignRules.MinFontSize, LabelDesignRules.MaxFontSize));
                    result.Bold = e.Bold ?? false;
                    result.Italic = e.Italic ?? false;
                    result.Align = Choice(e.Align, LabelDesignRules.Aligns, "center");
                    result.VAlign = Choice(e.VAlign, LabelDesignRules.VAligns, "middle");
                    result.Color = ColorOr(e.Color, LabelDesignRules.DefaultTextColor);
                    result.Background = ColorOr(e.Background, string.Empty);
                    break;

                case LabelDesignRules.ElementImage:
                    // An image removed from the library leaves an empty box rather than a broken element.
                    result.MediaId = e.MediaId is Guid mediaId && mediaIds.Contains(mediaId) ? mediaId : null;
                    break;

                case LabelDesignRules.ElementQr:
                    var qrText = (e.Text ?? string.Empty).Trim();
                    if (qrText.Length > LabelDesignRules.MaxQrTextLength)
                    {
                        error = $"Teksten i en QR-kode må højst være {LabelDesignRules.MaxQrTextLength} tegn.";
                        return null;
                    }
                    result.Text = qrText;
                    result.Color = ColorOr(e.Color, "#000000");
                    break;
            }
            return result;
        }

        private static double Round(double value) => Math.Round(value, 2);

        private static string Affix(string? value)
        {
            var text = (value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", " ");
            return text.Length > LabelDesignRules.MaxSerialAffixLength ? text[..LabelDesignRules.MaxSerialAffixLength] : text;
        }

        private static string Choice(string? value, IReadOnlySet<string> allowed, string fallback)
            => value is not null && allowed.Contains(value) ? value : fallback;

        private static string ColorOr(string? value, string fallback)
            => value is not null && HexColor.IsMatch(value) ? value.ToLowerInvariant() : fallback;

        private static LabelCollectionDesignDto ToDesignDto(LabelCollection collection) => new()
        {
            Name = collection.Name,
            Across = collection.Across,
            Down = collection.Down,
            Landscape = collection.Landscape,
            Labels = collection.Items.OrderBy(i => i.Order).Select(i => new LabelDesignDto
            {
                Quantity = i.Quantity,
                Background = i.Background,
                Elements = DeserializeElements(i.ElementsJson)
            }).ToList()
        };

        private static List<LabelElementDto> DeserializeElements(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<LabelElementDto>>(json, JsonOptions) ?? new();
            }
            catch (JsonException)
            {
                return new();
            }
        }

        private static LabelMediaDto ToMediaDto(LabelCollectionMedia media) => new()
        {
            Id = media.PublicId,
            Name = media.FileMetadata.OriginalFileName,
            SizeBytes = media.FileMetadata.FileSizeBytes
        };

        // ─── Billeder ────────────────────────────────────────────────────────────

        public async Task<LabelCollectionActionResultDto> UploadMediaAsync(int id, Stream content, string fileName, long length, string? ownerId, CancellationToken ct = default)
        {
            if (!await _context.LabelCollections.AnyAsync(c => c.Id == id, ct))
                return LabelCollectionActionResultDto.Fail("Label-samlingen blev ikke fundet.");

            // The content type comes from our own list, never from the browser.
            var ext = Path.GetExtension(fileName);
            if (!LabelDesignRules.ImageTypes.TryGetValue(ext, out var contentType))
                return LabelCollectionActionResultDto.Fail("Filtypen understøttes ikke. Brug et billede i PNG, JPG eller WEBP.");
            if (length <= 0)
                return LabelCollectionActionResultDto.Fail("Filen er tom.");
            if (length > LabelDesignRules.MaxImageBytes)
                return LabelCollectionActionResultDto.Fail($"Filen er for stor. Billeder må højst være {LabelDesignRules.MaxImageBytes / (1024 * 1024)} MB.");

            var relativeDir = Path.Combine(FilesRoot, FileCategories.Labels, id.ToString(CultureInfo.InvariantCulture));
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
                Category = FileCategories.Labels,
                CreatedAtUtc = DateTime.UtcNow
            };
            var media = new LabelCollectionMedia { LabelCollectionId = id, FileMetadata = metadata, CreatedAtUtc = DateTime.UtcNow };
            _context.LabelCollectionMedia.Add(media);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Label collection {CollectionId} got image {FileName}", id, storedFileName);
            var result = LabelCollectionActionResultDto.Ok($"\"{metadata.OriginalFileName}\" er uploadet.", id);
            result.Media = ToMediaDto(media);
            return result;
        }

        public async Task<LabelCollectionActionResultDto> DeleteMediaAsync(int id, Guid mediaId, CancellationToken ct = default)
        {
            var media = await _context.LabelCollectionMedia
                .Include(m => m.FileMetadata)
                .FirstOrDefaultAsync(m => m.LabelCollectionId == id && m.PublicId == mediaId, ct);
            if (media is null)
                return LabelCollectionActionResultDto.Fail("Billedet blev ikke fundet.");

            var key = mediaId.ToString();
            if (await _context.LabelCollectionItems.AnyAsync(i => i.LabelCollectionId == id && i.ElementsJson.Contains(key), ct))
                return LabelCollectionActionResultDto.Fail("Billedet bruges på en gemt label. Fjern det fra labelen og gem først.");

            var storedPath = media.FileMetadata.StoredPath;
            // The media row goes with the cascade delete.
            _context.FileMetadata.Remove(media.FileMetadata);
            await _context.SaveChangesAsync(ct);
            DeletePhysicalFile(storedPath);
            return LabelCollectionActionResultDto.Ok("Billedet er slettet.", id);
        }

        public async Task<(string FullPath, string ContentType)?> GetMediaFileAsync(Guid mediaId, CancellationToken ct = default)
        {
            var file = await _context.LabelCollectionMedia.AsNoTracking()
                .Where(m => m.PublicId == mediaId && !m.FileMetadata.IsDeleted)
                .Select(m => new { m.FileMetadata.StoredPath, m.FileMetadata.ContentType })
                .FirstOrDefaultAsync(ct);
            if (file is null)
                return null;

            var fullPath = Path.Combine(_env.ContentRootPath, file.StoredPath);
            return File.Exists(fullPath) ? (fullPath, file.ContentType) : null;
        }

        // ─── Print ───────────────────────────────────────────────────────────────

        public async Task<LabelPdfResult?> BuildPdfAsync(int id, bool cutMarks, CancellationToken ct = default)
        {
            var collection = await _context.LabelCollections.AsNoTracking()
                .Include(c => c.Items)
                .Include(c => c.Media).ThenInclude(m => m.FileMetadata)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
            if (collection is null)
                return null;

            var design = ToDesignDto(collection);
            var copies = design.Labels.Sum(l => l.Quantity);
            if (copies == 0)
                return LabelPdfResult.Fail("Der er ingen labels i samlingen. Design en label først.");
            if (copies > LabelRules.MaxLabelsPerPdf)
                return LabelPdfResult.Fail($"Der er {copies} labels, men én PDF kan højst have {LabelRules.MaxLabelsPerPdf}. Sæt antallet ned på nogle af labelsene.");

            var images = collection.Media
                .Where(m => !m.FileMetadata.IsDeleted)
                .ToDictionary(m => m.PublicId, m => Path.Combine(_env.ContentRootPath, m.FileMetadata.StoredPath));
            var title = $"Labels - {collection.Name}";
            byte[] content;
            try
            {
                content = LabelDesignPdf.Build(title, design, images, cutMarks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Designed labels for collection {CollectionId} could not be generated", id);
                return LabelPdfResult.Fail("PDF'en med labels kunne ikke dannes.");
            }

            return new LabelPdfResult
            {
                Success = true,
                Content = content,
                Title = title,
                FileName = FileNames.Sanitize(title, "labels") + ".pdf"
            };
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
                _logger.LogWarning(ex, "Label image file {StoredPath} could not be deleted", storedPath);
            }
        }
    }
}
