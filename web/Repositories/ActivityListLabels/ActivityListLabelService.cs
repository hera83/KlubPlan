using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Repositories.ActivityListLabels.Dtos;
using web.Repositories.ActivityListLabels.Interfaces;
using web.Repositories.ActivityLists;
using web.Repositories.ActivityLists.Dtos;
using web.Repositories.ActivityLists.Interfaces;
using web.ViewModels;

namespace web.Repositories.ActivityListLabels
{
    public class ActivityListLabelService : IActivityListLabelService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityListService _listService;
        private readonly ILogger<ActivityListLabelService> _logger;

        public ActivityListLabelService(ApplicationDbContext context, IActivityListService listService, ILogger<ActivityListLabelService> logger)
        {
            _context = context;
            _listService = listService;
            _logger = logger;
        }

        private sealed record PrintLabel(int ItemId, string Text, int Quantity);

        // ─── Labels på en linje ──────────────────────────────────────────────────

        public async Task<ActivityListItemLabelsViewModel?> GetItemLabelsAsync(int listId, int itemId, CancellationToken ct = default)
        {
            return await _context.ActivityListItems.AsNoTracking()
                .Where(i => i.Id == itemId && i.ActivityListId == listId)
                .Select(i => new ActivityListItemLabelsViewModel
                {
                    ItemId = i.Id,
                    RowNumber = i.Order,
                    Labels = i.Labels.OrderBy(l => l.Order).Select(l => new ActivityListLabelInput { Text = l.Text, Quantity = l.Quantity }).ToList()
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<ActivityListActionResultDto> SaveItemLabelsAsync(ActivityListItemLabelsSaveViewModel input, CancellationToken ct = default)
        {
            var item = await _context.ActivityListItems
                .Include(i => i.Labels)
                .FirstOrDefaultAsync(i => i.Id == input.ItemId && i.ActivityListId == input.ListId, ct);
            if (item is null)
                return ActivityListActionResultDto.Fail("Linjen blev ikke fundet.");

            var rows = input.Labels
                .Select(l => (Text: NormalizeText(l.Text), l.Quantity))
                .Where(l => l.Text.Length > 0)
                .ToList();

            if (rows.Count > ActivityListRules.MaxLabelsPerItem)
                return ActivityListActionResultDto.Fail($"En linje kan højst have {ActivityListRules.MaxLabelsPerItem} labels.");
            if (rows.Any(r => r.Text.Length > ActivityListRules.MaxLabelTextLength))
                return ActivityListActionResultDto.Fail($"Teksten på en label må højst være {ActivityListRules.MaxLabelTextLength} tegn.");
            if (rows.Any(r => r.Quantity is < 1 or > ActivityListRules.MaxLabelQuantity))
                return ActivityListActionResultDto.Fail($"Antal skal være mellem 1 og {ActivityListRules.MaxLabelQuantity}.");

            var existing = item.Labels.OrderBy(l => l.Order).ToList();
            if (rows.Count == 0 && existing.Count == 0)
                return ActivityListActionResultDto.Fail("Skriv en tekst på labelen.");

            // Reuse the existing rows in order (keeps CreatedAtUtc); extra rows are added or removed.
            var now = DateTime.UtcNow;
            for (var n = 0; n < rows.Count; n++)
            {
                var (text, quantity) = rows[n];
                if (n < existing.Count)
                {
                    var label = existing[n];
                    if (label.Text == text && label.Quantity == quantity && label.Order == n)
                        continue;
                    label.Text = text;
                    label.Quantity = quantity;
                    label.Order = n;
                    label.UpdatedAtUtc = now;
                }
                else
                {
                    item.Labels.Add(new ActivityListLabel { Text = text, Quantity = quantity, Order = n, CreatedAtUtc = now });
                }
            }
            _context.ActivityListLabels.RemoveRange(existing.Skip(rows.Count));

            await _context.SaveChangesAsync(ct);

            var copies = rows.Sum(r => r.Quantity);
            return ActivityListActionResultDto.Ok(item.Id, rows.Count == 0
                ? $"Labels på linje {item.Order} er fjernet."
                : $"Labels på linje {item.Order} er gemt ({copies} stk.).");
        }

        // ─── Print labels ────────────────────────────────────────────────────────

        public async Task<ActivityListLabelCountDto?> CountAsync(ActivityListItemFilterViewModel filter, string? userId, CancellationToken ct = default)
        {
            var loaded = await LoadPrintLabelsAsync(filter, userId, ct);
            if (loaded is null)
                return null;

            var labels = loaded.Value.Labels;
            return new ActivityListLabelCountDto
            {
                Lines = labels.Select(l => l.ItemId).Distinct().Count(),
                Copies = labels.Sum(l => l.Quantity)
            };
        }

        public async Task<ActivityListLabelPdfDto?> BuildPdfAsync(ActivityListLabelPrintViewModel input, string? userId, CancellationToken ct = default)
        {
            var loaded = await LoadPrintLabelsAsync(input, userId, ct);
            if (loaded is null)
                return null;

            var (title, labels) = loaded.Value;
            var copies = labels.Sum(l => l.Quantity);
            if (copies == 0)
                return ActivityListLabelPdfDto.Fail(input.HasFilter
                    ? "Der er ingen labels på linjerne i det nuværende filter."
                    : "Der er ingen labels at printe. Tilføj labels på linjerne først.");
            if (copies > ActivityListRules.MaxLabelsPerPdf)
                return ActivityListLabelPdfDto.Fail($"Der er {copies} labels, men én PDF kan højst have {ActivityListRules.MaxLabelsPerPdf}. Brug filteret og print dem i flere omgange.");

            var texts = labels.SelectMany(l => Enumerable.Repeat(l.Text, l.Quantity)).ToList();
            var pdfTitle = $"Labels - {title}";
            byte[] content;
            try
            {
                content = ActivityListLabelPdf.Build(pdfTitle, texts, input.Across, input.Down, input.Landscape);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Labels for list {ListId} could not be generated ({Across}x{Down})", input.ListId, input.Across, input.Down);
                return ActivityListLabelPdfDto.Fail("PDF'en med labels kunne ikke dannes.");
            }

            return new ActivityListLabelPdfDto
            {
                Success = true,
                Content = content,
                Title = pdfTitle,
                FileName = ActivityListService.SanitizeFileName(pdfTitle) + ".pdf"
            };
        }

        /// <summary>The list title and the labels on the lines matching the filter, in line order (the filter's sort) and then label order.</summary>
        private async Task<(string Title, List<PrintLabel> Labels)?> LoadPrintLabelsAsync(ActivityListItemFilterViewModel filter, string? userId, CancellationToken ct)
        {
            var itemIds = await _listService.GetItemIdsAsync(filter, userId, ct);
            if (itemIds is null)
                return null;

            var title = await _context.ActivityLists.Where(l => l.Id == filter.ListId).Select(l => l.Title).FirstAsync(ct);

            // All the list's labels, filtered in memory — avoids a huge IN (...) with thousands of line ids.
            var position = itemIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
            var labels = await _context.ActivityListLabels.AsNoTracking()
                .Where(l => l.Item.ActivityListId == filter.ListId)
                .Select(l => new { l.ActivityListItemId, l.Order, l.Text, l.Quantity })
                .ToListAsync(ct);

            var ordered = labels
                .Where(l => position.ContainsKey(l.ActivityListItemId))
                .OrderBy(l => position[l.ActivityListItemId])
                .ThenBy(l => l.Order)
                .Select(l => new PrintLabel(l.ActivityListItemId, l.Text, l.Quantity))
                .ToList();

            return (title, ordered);
        }

        private static string NormalizeText(string? text)
            => (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Trim();
    }
}
