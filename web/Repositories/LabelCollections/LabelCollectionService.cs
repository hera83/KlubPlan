using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Infrastructure.Labels;
using web.Repositories.LabelCollections.Dtos;
using web.Repositories.LabelCollections.Interfaces;
using web.ViewModels;

namespace web.Repositories.LabelCollections
{
    public class LabelCollectionService : ILabelCollectionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LabelCollectionService> _logger;

        public LabelCollectionService(ApplicationDbContext context, ILogger<LabelCollectionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<LabelCollectionFilterViewModel> GetCollectionsAsync(LabelCollectionFilterViewModel filter, CancellationToken ct = default)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize is < 10 or > 500 ? 10 : filter.PageSize;

            var query = _context.LabelCollections.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var search = filter.SearchText.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(search) || c.Items.Any(i => i.Text.ToLower().Contains(search)));
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
                    ChangedAtUtc = c.UpdatedAtUtc ?? c.CreatedAtUtc
                })
                .ToListAsync(ct);

            return filter;
        }

        public async Task<LabelCollectionEditViewModel?> GetForEditAsync(int id, CancellationToken ct = default)
        {
            return await _context.LabelCollections.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new LabelCollectionEditViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Labels = c.Items.OrderBy(i => i.Order).Select(i => new LabelInputViewModel { Text = i.Text, Quantity = i.Quantity }).ToList()
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<SaveLabelCollectionResponseDto> SaveAsync(LabelCollectionEditViewModel input, CancellationToken ct = default)
        {
            var name = input.Name.Trim();
            if (name.Length == 0)
                return SaveLabelCollectionResponseDto.Fail("Navn skal udfyldes.");

            var rows = LabelRows.Normalize(input.Labels);
            var error = LabelRows.Validate(rows, LabelRules.MaxLabelsPerCollection, $"En samling kan højst have {LabelRules.MaxLabelsPerCollection} labels.");
            if (error is not null)
                return SaveLabelCollectionResponseDto.Fail(error);

            LabelCollection collection;
            if (input.Id is int id)
            {
                var existing = await _context.LabelCollections.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id, ct);
                if (existing is null)
                    return SaveLabelCollectionResponseDto.Fail("Label-samlingen blev ikke fundet.");
                collection = existing;
                collection.Name = name;
                collection.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                collection = new LabelCollection { Name = name, CreatedAtUtc = DateTime.UtcNow };
                _context.LabelCollections.Add(collection);
            }

            LabelRows.Apply(collection.Items, rows,
                add: (row, order) => collection.Items.Add(new LabelCollectionItem { Text = row.Text, Quantity = row.Quantity, Order = order, CreatedAtUtc = DateTime.UtcNow }),
                remove: _context.LabelCollectionItems.RemoveRange);

            await _context.SaveChangesAsync(ct);

            return SaveLabelCollectionResponseDto.Ok(collection.Id, input.Id is null
                ? $"Label-samlingen \"{name}\" er oprettet."
                : $"Label-samlingen \"{name}\" er gemt.");
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var collection = await _context.LabelCollections.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (collection is null)
                return false;

            // The labels are removed by the cascade delete.
            _context.LabelCollections.Remove(collection);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<LabelPdfResult?> BuildPdfAsync(int id, LabelSheetViewModel sheet, CancellationToken ct = default)
        {
            var name = await _context.LabelCollections.Where(c => c.Id == id).Select(c => c.Name).FirstOrDefaultAsync(ct);
            if (name is null)
                return null;

            var labels = await _context.LabelCollectionItems.AsNoTracking()
                .Where(i => i.LabelCollectionId == id)
                .OrderBy(i => i.Order)
                .Select(i => new LabelRow(i.Text, i.Quantity))
                .ToListAsync(ct);

            return LabelSheetPdf.Create(name, labels, sheet, _logger,
                emptyMessage: "Der er ingen labels i samlingen. Tilføj labels under Rediger først.");
        }
    }
}
