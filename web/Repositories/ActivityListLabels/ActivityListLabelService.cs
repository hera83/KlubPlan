using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Infrastructure.Labels;
using web.Repositories.ActivityListLabels.Dtos;
using web.Repositories.ActivityListLabels.Interfaces;
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

        private sealed record PrintLabel(int ItemId, LabelRow Label);

        // ─── Labels på en linje ──────────────────────────────────────────────────

        public async Task<ActivityListItemLabelsViewModel?> GetItemLabelsAsync(int listId, int itemId, CancellationToken ct = default)
        {
            return await _context.ActivityListItems.AsNoTracking()
                .Where(i => i.Id == itemId && i.ActivityListId == listId)
                .Select(i => new ActivityListItemLabelsViewModel
                {
                    ItemId = i.Id,
                    RowNumber = i.Order,
                    Labels = i.Labels.OrderBy(l => l.Order).Select(l => new LabelInputViewModel { Text = l.Text, Quantity = l.Quantity }).ToList()
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

            var rows = LabelRows.Normalize(input.Labels);
            var error = LabelRows.Validate(rows, ActivityListRules.MaxLabelsPerItem, $"En linje kan højst have {ActivityListRules.MaxLabelsPerItem} labels.");
            if (error is not null)
                return ActivityListActionResultDto.Fail(error);
            if (rows.Count == 0 && item.Labels.Count == 0)
                return ActivityListActionResultDto.Fail("Skriv en tekst på labelen.");

            LabelRows.Apply(item.Labels, rows,
                add: (row, order) => item.Labels.Add(new ActivityListLabel { Text = row.Text, Quantity = row.Quantity, Order = order, CreatedAtUtc = DateTime.UtcNow }),
                remove: _context.ActivityListLabels.RemoveRange);

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
                Copies = labels.Sum(l => l.Label.Quantity)
            };
        }

        public async Task<LabelPdfResult?> BuildPdfAsync(ActivityListItemFilterViewModel filter, LabelSheetViewModel sheet, string? userId, CancellationToken ct = default)
        {
            var loaded = await LoadPrintLabelsAsync(filter, userId, ct);
            if (loaded is null)
                return null;

            var (title, labels) = loaded.Value;
            return LabelSheetPdf.Create(title, labels.Select(l => l.Label).ToList(), sheet, _logger,
                emptyMessage: filter.HasFilter
                    ? "Der er ingen labels på linjerne i det nuværende filter."
                    : "Der er ingen labels at printe. Tilføj labels på linjerne først.",
                tooManyHint: "Brug filteret og print dem i flere omgange.");
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
                .Select(l => new PrintLabel(l.ActivityListItemId, new LabelRow(l.Text, l.Quantity)))
                .ToList();

            return (title, ordered);
        }
    }
}
