using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Models;
using web.Infrastructure.Labels;
using web.Repositories.ActivityLists.Dtos;
using web.Repositories.ActivityLists.Interfaces;
using web.Repositories.ActivityLists.Mappers;

namespace web.Repositories.ActivityLists;

public class ActivityListRepository(WebDbContext dbContext, ILogger<ActivityListRepository> logger) : IActivityListRepository
{
    private const int ImportBatchSize = 500;
    private static readonly CultureInfo DanishCulture = CultureInfo.GetCultureInfo("da-DK");

    // ─── Lists ───────────────────────────────────────────────────────────────

    public async Task<List<ActivityListSummaryDto>> GetSummariesAsync(int activityId, string? userId, CancellationToken cancellationToken = default)
    {
        var lists = await dbContext.ActivityLists.AsNoTracking()
            .Where(l => l.ActivityId == activityId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                l.Id,
                l.Title,
                l.Description,
                l.SourceFileName,
                l.CreatedAt,
                CreatedBy = dbContext.Users.Where(u => u.Id == l.CreatedByUserId).Select(u => u.DisplayName).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        if (lists.Count == 0)
            return [];

        var listIds = lists.Select(l => l.Id).ToList();
        var myMemberIds = await GetMyMemberIdsAsync(activityId, userId, cancellationToken);

        var statuses = await dbContext.ActivityListStatuses.AsNoTracking()
            .Where(s => listIds.Contains(s.ActivityListId))
            .OrderBy(s => s.Order)
            .Select(s => new
            {
                s.ActivityListId,
                Status = new ActivityListStatusDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Color = s.Color,
                    IsDefault = s.IsDefault,
                    Count = dbContext.ActivityListItems.Count(i => i.StatusId == s.Id)
                }
            })
            .ToListAsync(cancellationToken);

        var counts = await dbContext.ActivityListItems
            .Where(i => listIds.Contains(i.ActivityListId))
            .GroupBy(i => i.ActivityListId)
            .Select(g => new
            {
                ListId = g.Key,
                Total = g.Count(),
                Unassigned = g.Count(i => i.AssignedToWorkgroupMemberId == null),
                Mine = g.Count(i => i.AssignedToWorkgroupMemberId != null && myMemberIds.Contains(i.AssignedToWorkgroupMemberId.Value))
            })
            .ToDictionaryAsync(c => c.ListId, cancellationToken);

        return lists.Select(l =>
        {
            counts.TryGetValue(l.Id, out var c);
            return new ActivityListSummaryDto
            {
                Id = l.Id,
                Title = l.Title,
                Description = l.Description,
                SourceFileName = l.SourceFileName,
                CreatedAt = l.CreatedAt,
                CreatedByName = l.CreatedBy,
                ItemCount = c?.Total ?? 0,
                UnassignedCount = c?.Unassigned ?? 0,
                MineCount = c?.Mine ?? 0,
                Statuses = statuses.Where(s => s.ActivityListId == l.Id).Select(s => s.Status).ToList()
            };
        }).ToList();
    }

    public async Task<ActivityListActionResultDto> CreateAsync(CreateActivityListDto dto, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Activities.AnyAsync(a => a.Id == dto.ActivityId, cancellationToken))
            return ActivityListActionResultDto.Fail("Aktiviteten blev ikke fundet.");

        var list = new ActivityList
        {
            ActivityId = dto.ActivityId,
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            SourceFileName = dto.SourceFileName,
            CreatedByUserId = dto.CreatedByUserId
        };
        var columns = dto.Headers
            .Select((name, i) => new ActivityListColumn { Name = name, Kind = ActivityListColumnKind.Imported, Order = i })
            .ToList();
        var statuses = ActivityListRules.DefaultStatuses
            .Select((s, i) => new ActivityListStatus { Name = s.Name, Color = s.Color, Order = i, IsDefault = i == 0 })
            .ToList();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        list.Columns = columns;
        list.Statuses = statuses;
        dbContext.ActivityLists.Add(list);
        await dbContext.SaveChangesAsync(cancellationToken);

        var defaultStatusId = statuses[0].Id;
        var columnIds = columns.Select(c => c.Id).ToArray();
        var listId = list.Id;

        // Inserted in batches with a cleared change tracker — keeps memory flat for big sheets.
        dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            for (var start = 0; start < dto.Rows.Count; start += ImportBatchSize)
            {
                var batch = dto.Rows.Skip(start).Take(ImportBatchSize).Select((cells, i) => new ActivityListItem
                {
                    ActivityListId = listId,
                    Order = start + i + 1,
                    StatusId = defaultStatusId,
                    Values = cells
                        .Select((value, c) => (value, c))
                        .Where(x => x.value is not null)
                        .Select(x => new ActivityListCellValue { ActivityListColumnId = columnIds[x.c], Value = x.value })
                        .ToList()
                });
                dbContext.ActivityListItems.AddRange(batch);
                await dbContext.SaveChangesAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();
            }
        }
        finally
        {
            dbContext.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Activity {ActivityId}: created list {ListId} '{Title}' from {FileName} ({Rows} rows, {Columns} columns)",
            dto.ActivityId, listId, list.Title, list.SourceFileName, dto.Rows.Count, columns.Count);

        return ActivityListActionResultDto.Ok(listId, $"Listen er oprettet med {dto.Rows.Count} linje{(dto.Rows.Count == 1 ? "" : "r")} og {columns.Count} kolonne{(columns.Count == 1 ? "" : "r")}.");
    }

    public async Task<ActivityListActionResultDto> UpdateAsync(int listId, string title, string? description, CancellationToken cancellationToken = default)
    {
        var list = await dbContext.ActivityLists.FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);
        if (list is null)
            return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

        list.Title = title.Trim();
        list.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        dbContext.Entry(list).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ActivityListActionResultDto.Ok(list.Id);
    }

    public async Task<ActivityListActionResultDto> DeleteAsync(int listId, CancellationToken cancellationToken = default)
    {
        var list = await dbContext.ActivityLists.AsNoTracking().FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);
        if (list is null)
            return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

        // Columns, statuses, lines, cell values and labels cascade in the database.
        await dbContext.ActivityLists.Where(l => l.Id == listId).ExecuteDeleteAsync(cancellationToken);
        logger.LogInformation("Activity {ActivityId}: deleted list {ListId} '{Title}'", list.ActivityId, listId, list.Title);
        return ActivityListActionResultDto.Ok(list.ActivityId);
    }

    // ─── List page ───────────────────────────────────────────────────────────

    public async Task<ActivityListDto?> GetDetailsAsync(int listId, string? userId, CancellationToken cancellationToken = default)
    {
        var loaded = await LoadSchemaAsync(listId, userId, cancellationToken);
        if (loaded is null)
            return null;

        var (list, schema, myMemberIds) = loaded.Value;
        var items = await QueryItemsAsync(new SearchActivityListItemsDto { ListId = listId }, schema, myMemberIds, cancellationToken);

        return new ActivityListDto
        {
            Id = list.Id,
            ActivityId = list.ActivityId,
            ActivityTitle = list.Activity.Title,
            Title = list.Title,
            Description = list.Description,
            SourceFileName = list.SourceFileName,
            CreatedAt = list.CreatedAt,
            TotalCount = schema.Statuses.Sum(s => s.Count) + await dbContext.ActivityListItems.CountAsync(i => i.ActivityListId == listId && i.StatusId == null, cancellationToken),
            UnassignedCount = await dbContext.ActivityListItems.CountAsync(i => i.ActivityListId == listId && i.AssignedToWorkgroupMemberId == null, cancellationToken),
            CurrentUserIsMember = myMemberIds.Count > 0,
            Schema = schema,
            Items = items
        };
    }

    public async Task<ActivityListCountsDto?> GetCountsAsync(int listId, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.ActivityLists.AnyAsync(l => l.Id == listId, cancellationToken))
            return null;

        var items = dbContext.ActivityListItems.Where(i => i.ActivityListId == listId);
        return new ActivityListCountsDto
        {
            Total = await items.CountAsync(cancellationToken),
            Unassigned = await items.CountAsync(i => i.AssignedToWorkgroupMemberId == null, cancellationToken),
            StatusCounts = await items
                .Where(i => i.StatusId != null)
                .GroupBy(i => i.StatusId!.Value)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken),
            MemberCounts = await items
                .Where(i => i.AssignedToWorkgroupMemberId != null)
                .GroupBy(i => i.AssignedToWorkgroupMemberId!.Value)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken)
        };
    }

    public async Task<ActivityListItemPageDto?> GetItemsAsync(SearchActivityListItemsDto search, string? userId, CancellationToken cancellationToken = default)
    {
        var loaded = await LoadSchemaAsync(search.ListId, userId, cancellationToken);
        if (loaded is null)
            return null;

        var (_, schema, myMemberIds) = loaded.Value;
        return await QueryItemsAsync(search, schema, myMemberIds, cancellationToken);
    }

    // ─── Lines ───────────────────────────────────────────────────────────────

    public Task<ActivityListFieldUpdateResultDto> UpdateFieldAsync(UpdateActivityListFieldDto dto, string? userId, CancellationToken cancellationToken = default)
        => UpdateFieldCoreAsync(dto, userId, null, cancellationToken);

    public async Task<ActivityListActionResultDto> SaveItemAsync(int listId, int? itemId, IDictionary<int, string?> values, string? userId, CancellationToken cancellationToken = default)
    {
        var list = await dbContext.ActivityLists
            .Include(l => l.Columns)
            .Include(l => l.Statuses)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);
        if (list is null)
            return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

        ActivityListItem? item;
        var isNew = itemId is null or 0;
        if (isNew)
        {
            var maxOrder = await dbContext.ActivityListItems.Where(i => i.ActivityListId == listId).MaxAsync(i => (int?)i.Order, cancellationToken) ?? 0;
            item = new ActivityListItem
            {
                ActivityListId = listId,
                Order = maxOrder + 1,
                StatusId = list.Statuses.FirstOrDefault(s => s.IsDefault)?.Id ?? list.Statuses.OrderBy(s => s.Order).FirstOrDefault()?.Id
            };
            dbContext.ActivityListItems.Add(item);
        }
        else
        {
            item = await dbContext.ActivityListItems.Include(i => i.Values).FirstOrDefaultAsync(i => i.Id == itemId && i.ActivityListId == listId, cancellationToken);
            if (item is null)
                return ActivityListActionResultDto.Fail("Linjen blev ikke fundet.");
        }

        foreach (var column in list.Columns)
        {
            if (!values.TryGetValue(column.Id, out var raw))
                continue;
            var value = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
            var error = NormalizeCellValue(column, ref value);
            if (error is not null)
                return ActivityListActionResultDto.Fail($"{column.Name}: {error}");
            SetCellValue(item, column.Id, value);
        }

        if (isNew && item.Values.All(v => v.Value is null))
            return ActivityListActionResultDto.Fail("Udfyld mindst ét felt på linjen.");

        item.UpdatedByUserId = userId;
        item.UpdatedByWorkgroupMemberId = null;
        if (!isNew)
            dbContext.Entry(item).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ActivityListActionResultDto.Ok(item.Id, isNew ? $"Linje {item.Order} er tilføjet." : $"Linje {item.Order} er gemt.");
    }

    public async Task<ActivityListActionResultDto> DeleteItemAsync(int listId, int itemId, CancellationToken cancellationToken = default)
    {
        var deleted = await dbContext.ActivityListItems.Where(i => i.Id == itemId && i.ActivityListId == listId).ExecuteDeleteAsync(cancellationToken);
        return deleted > 0 ? ActivityListActionResultDto.Ok(itemId) : ActivityListActionResultDto.Fail("Linjen blev ikke fundet.");
    }

    public async Task<ActivityListActionResultDto> DistributeAsync(int listId, IReadOnlyCollection<int> memberIds, bool onlyUnassigned, string? userId, CancellationToken cancellationToken = default)
    {
        var list = await dbContext.ActivityLists.AsNoTracking().FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);
        if (list is null)
            return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

        var members = (await LoadMembersAsync(list.ActivityId, null, cancellationToken)).Where(m => memberIds.Contains(m.Id)).ToList();
        if (members.Count == 0)
            return ActivityListActionResultDto.Fail("Vælg mindst én person fra arbejdsgruppen.");

        var items = await dbContext.ActivityListItems
            .Where(i => i.ActivityListId == listId && (!onlyUnassigned || i.AssignedToWorkgroupMemberId == null))
            .OrderBy(i => i.Order)
            .ToListAsync(cancellationToken);
        if (items.Count == 0)
            return ActivityListActionResultDto.Fail(onlyUnassigned ? "Alle linjer har allerede en tilknytning." : "Listen har ingen linjer.");

        // Existing lines count when only the unassigned are distributed, so the totals end up even —
        // e.g. a helper added later gets more of the remaining lines than those who already have some.
        var existing = new int[members.Count];
        if (onlyUnassigned)
        {
            var ids = members.Select(m => m.Id).ToList();
            var counts = await dbContext.ActivityListItems
                .Where(i => i.ActivityListId == listId && i.AssignedToWorkgroupMemberId != null && ids.Contains(i.AssignedToWorkgroupMemberId.Value))
                .GroupBy(i => i.AssignedToWorkgroupMemberId!.Value)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
            for (var m = 0; m < members.Count; m++)
                existing[m] = counts.GetValueOrDefault(members[m].Id);
        }

        var quota = new int[members.Count];
        var totals = (int[])existing.Clone();
        for (var n = 0; n < items.Count; n++)
        {
            var next = Array.IndexOf(totals, totals.Min());
            totals[next]++;
            quota[next]++;
        }

        // Contiguous blocks in list order — useful when the sheet is sorted by e.g. address.
        var index = 0;
        for (var m = 0; m < members.Count; m++)
        {
            for (var q = 0; q < quota[m]; q++, index++)
            {
                items[index].AssignedToWorkgroupMemberId = members[m].Id;
                items[index].UpdatedByUserId = userId;
                items[index].UpdatedByWorkgroupMemberId = null;
                dbContext.Entry(items[index]).State = EntityState.Modified;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var summary = string.Join(", ", members.Select((m, i) => $"{m.Name} {quota[i]}"));
        logger.LogInformation("List {ListId}: distributed {Count} line(s) between {Members} member(s)", listId, items.Count, members.Count);
        return ActivityListActionResultDto.Ok(listId, $"{items.Count} linje{(items.Count == 1 ? "" : "r")} fordelt: {summary}.");
    }

    // ─── Columns & statuses ──────────────────────────────────────────────────

    public async Task<ActivityListActionResultDto> SaveColumnAsync(SaveActivityListColumnDto dto, CancellationToken cancellationToken = default)
    {
        var list = await dbContext.ActivityLists.Include(l => l.Columns).FirstOrDefaultAsync(l => l.Id == dto.ListId, cancellationToken);
        if (list is null)
            return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

        var name = dto.Name.Trim();
        if (name.Length == 0)
            return ActivityListActionResultDto.Fail("Angiv et kolonnenavn.");
        if (list.Columns.Any(c => c.Id != dto.ColumnId && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            return ActivityListActionResultDto.Fail($"Der findes allerede en kolonne med navnet \"{name}\".");

        var column = dto.ColumnId.HasValue ? list.Columns.FirstOrDefault(c => c.Id == dto.ColumnId.Value) : null;
        if (dto.ColumnId.HasValue && column is null)
            return ActivityListActionResultDto.Fail("Kolonnen blev ikke fundet.");

        var kind = column?.Kind ?? dto.Kind;
        if (column is null && kind == ActivityListColumnKind.Imported)
            return ActivityListActionResultDto.Fail("Vælg en kolonnetype.");

        string? options = null;
        if (kind == ActivityListColumnKind.Choice)
        {
            var parsed = ActivityListMapper.ParseOptions(dto.Options);
            if (parsed.Count == 0)
                return ActivityListActionResultDto.Fail("En valgliste skal have mindst ét valg.");
            options = string.Join('\n', parsed);
        }

        if (column is null)
        {
            column = new ActivityListColumn
            {
                ActivityListId = list.Id,
                Name = name,
                Kind = kind,
                Options = options,
                Order = list.Columns.Count == 0 ? 0 : list.Columns.Max(c => c.Order) + 1
            };
            dbContext.ActivityListColumns.Add(column);
        }
        else
        {
            column.Name = name;
            if (kind == ActivityListColumnKind.Choice)
                column.Options = options;
        }

        dbContext.Entry(list).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ActivityListActionResultDto.Ok(column.Id, dto.ColumnId.HasValue ? "Kolonnen er gemt." : $"Kolonnen \"{name}\" er tilføjet.");
    }

    public async Task<ActivityListActionResultDto> DeleteColumnAsync(int listId, int columnId, CancellationToken cancellationToken = default)
    {
        var column = await dbContext.ActivityListColumns.FirstOrDefaultAsync(c => c.Id == columnId && c.ActivityListId == listId, cancellationToken);
        if (column is null)
            return ActivityListActionResultDto.Fail("Kolonnen blev ikke fundet.");
        if (column.Kind == ActivityListColumnKind.Imported)
            return ActivityListActionResultDto.Fail("Kolonner fra Excel-arket kan ikke slettes.");

        dbContext.ActivityListColumns.Remove(column);
        await dbContext.SaveChangesAsync(cancellationToken);
        // A counted column that's deleted also removes the "Optælling" card.
        await dbContext.ActivityLists
            .Where(l => l.Id == listId && l.SumColumnId == columnId)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.SumColumnId, (int?)null), cancellationToken);
        return ActivityListActionResultDto.Ok(columnId, $"Kolonnen \"{column.Name}\" er slettet.");
    }

    public async Task<ActivityListActionResultDto> SetColumnHiddenAsync(int listId, int columnId, bool hidden, CancellationToken cancellationToken = default)
    {
        var column = await dbContext.ActivityListColumns.FirstOrDefaultAsync(c => c.Id == columnId && c.ActivityListId == listId, cancellationToken);
        if (column is null)
            return ActivityListActionResultDto.Fail("Kolonnen blev ikke fundet.");

        column.IsHidden = hidden;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ActivityListActionResultDto.Ok(columnId, hidden
            ? $"Kolonnen \"{column.Name}\" er skjult i tabellen. Den kommer stadig med ved eksport."
            : $"Kolonnen \"{column.Name}\" vises igen i tabellen.");
    }

    public async Task<ActivityListActionResultDto> SetNoteVisibleAsync(int listId, bool visible, CancellationToken cancellationToken = default)
    {
        var list = await dbContext.ActivityLists.FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);
        if (list is null)
            return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

        list.ShowNote = visible;
        dbContext.Entry(list).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ActivityListActionResultDto.Ok(listId, visible ? "Note-kolonnen er slået til." : "Note-kolonnen er fjernet. Eksisterende noter gemmes, hvis den slås til igen.");
    }

    public async Task<ActivityListActionResultDto> SaveStatusesAsync(SaveActivityListStatusesDto dto, CancellationToken cancellationToken = default)
    {
        var list = await dbContext.ActivityLists.Include(l => l.Statuses).FirstOrDefaultAsync(l => l.Id == dto.ListId, cancellationToken);
        if (list is null)
            return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

        var rows = dto.Statuses.Select(s => new { s.Id, Name = s.Name?.Trim() ?? string.Empty, s.Color }).ToList();
        if (rows.Count == 0)
            return ActivityListActionResultDto.Fail("Listen skal have mindst én status.");
        if (rows.Any(r => r.Name.Length == 0))
            return ActivityListActionResultDto.Fail("Alle statusser skal have et navn.");
        if (rows.Any(r => r.Name.Length > 50))
            return ActivityListActionResultDto.Fail("Et statusnavn må højst være 50 tegn.");
        if (rows.GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            return ActivityListActionResultDto.Fail("To statusser kan ikke have samme navn.");
        if (rows.Any(r => !ActivityListRules.IsValidColor(r.Color)))
            return ActivityListActionResultDto.Fail("Ukendt farve.");

        var defaultIndex = Math.Clamp(dto.DefaultIndex, 0, rows.Count - 1);
        var kept = new List<ActivityListStatus>();
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var status = row.Id is > 0 ? list.Statuses.FirstOrDefault(s => s.Id == row.Id) : null;
            if (status is null)
            {
                status = new ActivityListStatus { ActivityListId = list.Id };
                list.Statuses.Add(status);
            }
            status.Name = row.Name;
            status.Color = row.Color;
            status.Order = i;
            status.IsDefault = i == defaultIndex;
            kept.Add(status);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var removed = list.Statuses.Except(kept).ToList();
        if (removed.Count > 0)
        {
            var removedIds = removed.Select(s => s.Id).ToList();
            var defaultId = kept[defaultIndex].Id;
            await dbContext.ActivityListItems
                .Where(i => i.ActivityListId == list.Id && i.StatusId != null && removedIds.Contains(i.StatusId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.StatusId, defaultId), cancellationToken);
            dbContext.ActivityListStatuses.RemoveRange(removed);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return ActivityListActionResultDto.Ok(list.Id, "Statusserne er gemt.");
    }

    // ─── Export & Optælling ──────────────────────────────────────────────────

    public async Task<ActivityListExportDataDto?> GetExportDataAsync(SearchActivityListItemsDto search, string? userId, CancellationToken cancellationToken = default)
    {
        var loaded = await LoadSchemaAsync(search.ListId, userId, cancellationToken);
        if (loaded is null)
            return null;

        var (list, schema, myMemberIds) = loaded.Value;
        return new ActivityListExportDataDto
        {
            Title = list.Title,
            Schema = schema,
            Rows = await ToRowsAsync(Sort(BuildQuery(search, schema, myMemberIds), search), cancellationToken)
        };
    }

    public async Task<ActivityListColumnSumDto?> SumColumnAsync(SearchActivityListItemsDto search, int columnId, string? userId, CancellationToken cancellationToken = default)
    {
        var loaded = await LoadSchemaAsync(search.ListId, userId, cancellationToken);
        if (loaded is null)
            return null;

        var (_, schema, myMemberIds) = loaded.Value;
        var column = schema.Columns.FirstOrDefault(c => c.Id == columnId);
        if (column is null)
            return null;
        if (column.Kind == ActivityListColumnKind.YesNo)
            return ActivityListColumnSumDto.Fail("En Ja/nej-kolonne kan ikke tælles op.");

        // Same lines as the table shows for the search — a sub-query, so no huge IN (...) with line ids.
        var itemIds = BuildQuery(search, schema, myMemberIds).Select(i => i.Id);
        var total = await itemIds.CountAsync(cancellationToken);
        var values = await dbContext.ActivityListCellValues.AsNoTracking()
            .Where(v => v.ActivityListColumnId == columnId && v.Value != null && itemIds.Contains(v.ActivityListItemId))
            .Select(v => v.Value!)
            .ToListAsync(cancellationToken);

        // Lines without a stored value (empty cells) never reach the loop — they count as skipped too.
        var sum = 0m;
        var counted = 0;
        foreach (var value in values)
        {
            if (!TryParseWholeNumber(value, out var number))
                continue;
            try
            {
                sum += number;
                counted++;
            }
            catch (OverflowException)
            {
                // Absurdly large numbers are skipped rather than breaking the whole count.
            }
        }

        return new ActivityListColumnSumDto
        {
            Success = true,
            ColumnId = column.Id,
            ColumnName = column.Name,
            Sum = sum,
            SumText = sum.ToString("N0", DanishCulture),
            Counted = counted,
            Skipped = total - counted,
            Filtered = search.HasFilter
        };
    }

    public async Task<ActivityListActionResultDto> SetSumColumnAsync(int listId, int? columnId, CancellationToken cancellationToken = default)
    {
        var list = await dbContext.ActivityLists.FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);
        if (list is null)
            return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

        if (columnId is null)
        {
            list.SumColumnId = null;
            dbContext.Entry(list).State = EntityState.Modified;
            await dbContext.SaveChangesAsync(cancellationToken);
            return ActivityListActionResultDto.Ok(listId, "Optællingen er fjernet.");
        }

        var column = await dbContext.ActivityListColumns.AsNoTracking()
            .Where(c => c.Id == columnId && c.ActivityListId == listId)
            .Select(c => new { c.Name, c.Kind })
            .FirstOrDefaultAsync(cancellationToken);
        if (column is null)
            return ActivityListActionResultDto.Fail("Kolonnen blev ikke fundet.");
        if (column.Kind == ActivityListColumnKind.YesNo)
            return ActivityListActionResultDto.Fail("En Ja/nej-kolonne kan ikke tælles op.");

        list.SumColumnId = columnId;
        dbContext.Entry(list).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ActivityListActionResultDto.Ok(listId, $"Optælling af \"{column.Name}\" er gemt på listen.");
    }

    // ─── Send links ──────────────────────────────────────────────────────────

    public async Task<ActivityListLinkRecipientsDto?> GetLinkRecipientsAsync(int listId, IReadOnlyCollection<int> memberIds, CancellationToken cancellationToken = default)
    {
        var list = await dbContext.ActivityLists.AsNoTracking()
            .Where(l => l.Id == listId)
            .Select(l => new { l.Id, l.Title, l.ActivityId, ActivityTitle = l.Activity.Title })
            .FirstOrDefaultAsync(cancellationToken);
        if (list is null)
            return null;

        // Only external contacts — administrators in the workgroup log in and use the list page.
        var members = await dbContext.ActivityWorkgroupMembers.AsNoTracking()
            .Where(m => m.ActivityId == list.ActivityId && m.ApplicationUserId == null && memberIds.Contains(m.Id))
            .OrderBy(m => m.Order)
            .Select(m => new ActivityListLinkMemberDto
            {
                Id = m.Id,
                Name = m.Name ?? string.Empty,
                Email = m.Email,
                Mobile = m.Mobile,
                PublicId = m.PublicId,
                AssignedCount = dbContext.ActivityListItems.Count(i => i.ActivityListId == list.Id && i.AssignedToWorkgroupMemberId == m.Id)
            })
            .ToListAsync(cancellationToken);

        return new ActivityListLinkRecipientsDto
        {
            ListId = list.Id,
            ListTitle = list.Title,
            ActivityTitle = list.ActivityTitle,
            Members = members
        };
    }

    // ─── Offentligt link (/Arbejdsliste) ─────────────────────────────────────

    public async Task<ActivityListPublicDto> GetPublicListAsync(int listId, Guid memberPublicId, CancellationToken cancellationToken = default)
    {
        var resolved = await ResolvePublicAsync(listId, memberPublicId, cancellationToken);
        if (resolved is null)
            return new ActivityListPublicDto { Found = false };

        var (list, member, schema) = resolved.Value;
        var items = await QueryPublicItemsAsync(new SearchActivityListItemsDto { ListId = listId }, member, schema, cancellationToken);

        return new ActivityListPublicDto
        {
            Found = true,
            ListId = list.Id,
            MemberPublicId = member.PublicId,
            MemberName = member.Name ?? string.Empty,
            ActivityTitle = list.Activity.Title,
            Title = list.Title,
            Description = list.Description,
            Schema = schema,
            Items = items
        };
    }

    public async Task<ActivityListItemPageDto?> GetPublicItemsAsync(Guid memberPublicId, SearchActivityListItemsDto search, CancellationToken cancellationToken = default)
    {
        var resolved = await ResolvePublicAsync(search.ListId, memberPublicId, cancellationToken);
        if (resolved is null)
            return null;

        var (_, member, schema) = resolved.Value;
        return await QueryPublicItemsAsync(search, member, schema, cancellationToken);
    }

    public async Task<ActivityListFieldUpdateResultDto> UpdatePublicFieldAsync(Guid memberPublicId, UpdateActivityListFieldDto dto, CancellationToken cancellationToken = default)
    {
        var member = await FindPublicMemberAsync(dto.ListId, memberPublicId, cancellationToken);
        if (member is null)
            return new ActivityListFieldUpdateResultDto { Success = false, ErrorMessage = "Linket er ikke længere gyldigt." };

        return await UpdateFieldCoreAsync(dto, null, member, cancellationToken);
    }

    // ─── Labels ──────────────────────────────────────────────────────────────

    public Task<ActivityListItemLabelsDto?> GetItemLabelsAsync(int listId, int itemId, CancellationToken cancellationToken = default) =>
        dbContext.ActivityListItems.AsNoTracking()
            .Where(i => i.Id == itemId && i.ActivityListId == listId)
            .Select(i => new ActivityListItemLabelsDto
            {
                ItemId = i.Id,
                RowNumber = i.Order,
                Labels = i.Labels.OrderBy(l => l.Order).Select(l => new ActivityListLabelDto { Text = l.Text, Quantity = l.Quantity }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ActivityListActionResultDto> SaveItemLabelsAsync(int listId, int itemId, List<ActivityListLabelDto> labels, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.ActivityListItems
            .Include(i => i.Labels)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.ActivityListId == listId, cancellationToken);
        if (item is null)
            return ActivityListActionResultDto.Fail("Linjen blev ikke fundet.");

        var rows = labels.Select(l => new LabelRow(l.Text, l.Quantity)).ToList();
        var error = LabelRows.Validate(rows, ActivityListRules.MaxLabelsPerItem, $"En linje kan højst have {ActivityListRules.MaxLabelsPerItem} labels.");
        if (error is not null)
            return ActivityListActionResultDto.Fail(error);
        if (rows.Count == 0 && item.Labels.Count == 0)
            return ActivityListActionResultDto.Fail("Skriv en tekst på labelen.");

        LabelRows.Apply(item.Labels, rows,
            add: (row, order) => item.Labels.Add(new ActivityListLabel { Text = row.Text, Quantity = row.Quantity, Order = order }),
            remove: dbContext.ActivityListLabels.RemoveRange);

        await dbContext.SaveChangesAsync(cancellationToken);

        var copies = rows.Sum(r => r.Quantity);
        return ActivityListActionResultDto.Ok(item.Id, rows.Count == 0
            ? $"Labels på linje {item.Order} er fjernet."
            : $"Labels på linje {item.Order} er gemt ({copies} stk.).");
    }

    public async Task<ActivityListPrintLabelsDto?> GetPrintLabelsAsync(SearchActivityListItemsDto search, string? userId, CancellationToken cancellationToken = default)
    {
        var loaded = await LoadSchemaAsync(search.ListId, userId, cancellationToken);
        if (loaded is null)
            return null;

        var (list, schema, myMemberIds) = loaded.Value;
        var itemIds = await Sort(BuildQuery(search, schema, myMemberIds), search).Select(i => i.Id).ToListAsync(cancellationToken);

        // All the list's labels, filtered in memory — avoids a huge IN (...) with thousands of line ids.
        var position = itemIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        var labels = await dbContext.ActivityListLabels.AsNoTracking()
            .Where(l => l.Item.ActivityListId == search.ListId)
            .Select(l => new { l.ActivityListItemId, l.Order, l.Text, l.Quantity })
            .ToListAsync(cancellationToken);

        return new ActivityListPrintLabelsDto
        {
            Title = list.Title,
            Labels = labels
                .Where(l => position.ContainsKey(l.ActivityListItemId))
                .OrderBy(l => position[l.ActivityListItemId])
                .ThenBy(l => l.Order)
                .Select(l => new ActivityListPrintLabelDto { ItemId = l.ActivityListItemId, Text = l.Text, Quantity = l.Quantity })
                .ToList()
        };
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Shared by the logged-in list page and the public /Arbejdsliste link. With actingMember set
    /// (an external contact), only lines assigned to that member can be changed, Tilknyttet can't be
    /// changed, and the returned counts cover the member's own lines only.
    /// </summary>
    private async Task<ActivityListFieldUpdateResultDto> UpdateFieldCoreAsync(UpdateActivityListFieldDto dto, string? userId, ActivityWorkgroupMember? actingMember, CancellationToken cancellationToken)
    {
        ActivityListFieldUpdateResultDto Fail(string message) => new() { Success = false, ErrorMessage = message };

        var list = await dbContext.ActivityLists.AsNoTracking().FirstOrDefaultAsync(l => l.Id == dto.ListId, cancellationToken);
        if (list is null)
            return Fail("Listen blev ikke fundet.");

        var item = await dbContext.ActivityListItems
            .Include(i => i.Values)
            .FirstOrDefaultAsync(i => i.Id == dto.ItemId && i.ActivityListId == dto.ListId, cancellationToken);
        if (item is null)
            return Fail("Linjen blev ikke fundet — den kan være slettet.");

        if (actingMember is not null)
        {
            if (item.AssignedToWorkgroupMemberId != actingMember.Id)
                return Fail("Linjen er ikke længere tildelt dig.");
            if (dto.Field == ActivityListRules.FieldAssigned)
                return Fail("Du kan ikke ændre, hvem linjen er tilknyttet.");
        }

        var value = string.IsNullOrWhiteSpace(dto.Value) ? null : dto.Value.Trim();

        switch (dto.Field)
        {
            case ActivityListRules.FieldStatus:
                if (!int.TryParse(value, out var statusId) || !await dbContext.ActivityListStatuses.AnyAsync(s => s.Id == statusId && s.ActivityListId == list.Id, cancellationToken))
                    return Fail("Ukendt status.");
                item.StatusId = statusId;
                break;

            case ActivityListRules.FieldNote:
                if (!list.ShowNote)
                    return Fail("Note-kolonnen er slået fra for denne liste.");
                if (value?.Length > ActivityListRules.MaxNoteLength)
                    return Fail($"Noten må højst være {ActivityListRules.MaxNoteLength} tegn.");
                item.Note = value;
                break;

            case ActivityListRules.FieldAssigned:
                if (value is null)
                {
                    item.AssignedToWorkgroupMemberId = null;
                }
                else
                {
                    if (!int.TryParse(value, out var memberId) || !await dbContext.ActivityWorkgroupMembers.AnyAsync(m => m.Id == memberId && m.ActivityId == list.ActivityId, cancellationToken))
                        return Fail("Personen er ikke i aktivitetens arbejdsgruppe.");
                    item.AssignedToWorkgroupMemberId = memberId;
                }
                break;

            case ActivityListRules.FieldColumn:
                var column = dto.ColumnId.HasValue
                    ? await dbContext.ActivityListColumns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == dto.ColumnId.Value && c.ActivityListId == list.Id, cancellationToken)
                    : null;
                if (column is null)
                    return Fail("Kolonnen blev ikke fundet.");
                var error = NormalizeCellValue(column, ref value);
                if (error is not null)
                    return Fail(error);
                SetCellValue(item, column.Id, value);
                break;

            default:
                return Fail("Ukendt felt.");
        }

        item.UpdatedByUserId = actingMember is null ? userId : null;
        item.UpdatedByWorkgroupMemberId = actingMember?.Id;
        dbContext.Entry(item).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);

        var actingMemberId = actingMember?.Id;
        var statusCounts = await dbContext.ActivityListItems
            .Where(i => i.ActivityListId == list.Id && i.StatusId != null && (actingMemberId == null || i.AssignedToWorkgroupMemberId == actingMemberId))
            .GroupBy(i => i.StatusId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);

        var updatedByName = actingMember is not null
            ? actingMember.Name
            : userId is null ? null : await dbContext.Users.Where(u => u.Id == userId).Select(u => u.DisplayName).FirstOrDefaultAsync(cancellationToken);

        return new ActivityListFieldUpdateResultDto
        {
            Success = true,
            StatusCounts = statusCounts,
            UnassignedCount = await dbContext.ActivityListItems.CountAsync(i => i.ActivityListId == list.Id && i.AssignedToWorkgroupMemberId == null, cancellationToken),
            UpdatedByName = updatedByName,
            UpdatedAtUtc = item.UpdatedAt
        };
    }

    private async Task<ActivityListItemPageDto> QueryItemsAsync(SearchActivityListItemsDto search, ActivityListSchemaDto schema, HashSet<int> myMemberIds, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, search.Page);
        var pageSize = search.PageSize is > 0 and <= 500 ? search.PageSize : 10;

        var query = BuildQuery(search, schema, myMemberIds);
        var total = await query.CountAsync(cancellationToken);
        if ((page - 1) * pageSize >= total && total > 0)
            page = (int)Math.Ceiling(total / (double)pageSize);

        var rows = await ToRowsAsync(Sort(query, search).Skip((page - 1) * pageSize).Take(pageSize), cancellationToken);

        return new ActivityListItemPageDto
        {
            ListId = search.ListId,
            SearchText = search.SearchText,
            StatusId = search.StatusId,
            Assigned = search.Assigned,
            NoteFilter = search.NoteFilter,
            SortColumn = search.SortColumn,
            SortDir = search.SortDir,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Schema = schema,
            Rows = rows
        };
    }

    private static async Task<List<ActivityListItemRowDto>> ToRowsAsync(IQueryable<ActivityListItem> query, CancellationToken cancellationToken)
    {
        var rows = await query
            .Select(i => new
            {
                i.Id,
                i.Order,
                i.StatusId,
                i.Note,
                i.AssignedToWorkgroupMemberId,
                // An imported line that nobody has touched has UpdatedAt == CreatedAt and no UpdatedBy.
                // A line added by hand has both timestamps equal too, but records who added it.
                UpdatedAt = i.UpdatedAt > i.CreatedAt || i.UpdatedByUserId != null || i.UpdatedByWorkgroupMemberId != null ? i.UpdatedAt : (DateTime?)null,
                UpdatedBy = i.UpdatedByUser != null ? i.UpdatedByUser.DisplayName
                    : i.UpdatedByWorkgroupMember != null ? i.UpdatedByWorkgroupMember.Name : null,
                Values = i.Values.Select(v => new { v.ActivityListColumnId, v.Value }).ToList(),
                LabelCopies = i.Labels.Sum(l => l.Quantity)
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new ActivityListItemRowDto
        {
            Id = r.Id,
            RowNumber = r.Order,
            StatusId = r.StatusId,
            Note = r.Note,
            AssignedMemberId = r.AssignedToWorkgroupMemberId,
            UpdatedAtUtc = r.UpdatedAt,
            UpdatedByName = r.UpdatedBy,
            Values = r.Values.ToDictionary(v => v.ActivityListColumnId, v => v.Value),
            LabelCopies = r.LabelCopies
        }).ToList();
    }

    /// <summary>An external contact (no login) in the list's activity workgroup, or null.</summary>
    private async Task<ActivityWorkgroupMember?> FindPublicMemberAsync(int listId, Guid memberPublicId, CancellationToken cancellationToken)
    {
        if (memberPublicId == Guid.Empty)
            return null;

        return await dbContext.ActivityWorkgroupMembers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.PublicId == memberPublicId
                && m.ApplicationUserId == null
                && dbContext.ActivityLists.Any(l => l.Id == listId && l.ActivityId == m.ActivityId), cancellationToken);
    }

    /// <summary>
    /// Gate for the public link + a schema trimmed for it: no other members (their names aren't
    /// shown) and status counts for the member's own lines only.
    /// </summary>
    private async Task<(ActivityList List, ActivityWorkgroupMember Member, ActivityListSchemaDto Schema)?> ResolvePublicAsync(int listId, Guid memberPublicId, CancellationToken cancellationToken)
    {
        var member = await FindPublicMemberAsync(listId, memberPublicId, cancellationToken);
        if (member is null)
            return null;

        var loaded = await LoadSchemaAsync(listId, null, cancellationToken);
        if (loaded is null)
            return null;

        var (list, schema, _) = loaded.Value;
        var ownCounts = await dbContext.ActivityListItems
            .Where(i => i.ActivityListId == listId && i.AssignedToWorkgroupMemberId == member.Id && i.StatusId != null)
            .GroupBy(i => i.StatusId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
        schema.Statuses.ForEach(s => s.Count = ownCounts.GetValueOrDefault(s.Id));
        schema.Members = [];

        return (list, member, schema);
    }

    private async Task<ActivityListItemPageDto> QueryPublicItemsAsync(SearchActivityListItemsDto search, ActivityWorkgroupMember member, ActivityListSchemaDto schema, CancellationToken cancellationToken)
    {
        // Always locked to the member's own lines, whatever the query string says.
        search.Assigned = member.Id.ToString(CultureInfo.InvariantCulture);
        var items = await QueryItemsAsync(search, schema, [], cancellationToken);
        items.Assigned = null;
        items.MemberPublicId = member.PublicId;
        return items;
    }

    private async Task<(ActivityList List, ActivityListSchemaDto Schema, HashSet<int> MyMemberIds)?> LoadSchemaAsync(int listId, string? userId, CancellationToken cancellationToken)
    {
        var list = await dbContext.ActivityLists.AsNoTracking()
            .Include(l => l.Activity)
            .Include(l => l.Columns)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);
        if (list is null)
            return null;

        var statuses = await dbContext.ActivityListStatuses.AsNoTracking()
            .Where(s => s.ActivityListId == listId)
            .OrderBy(s => s.Order)
            .Select(s => new ActivityListStatusDto
            {
                Id = s.Id,
                Name = s.Name,
                Color = s.Color,
                IsDefault = s.IsDefault,
                Count = dbContext.ActivityListItems.Count(i => i.StatusId == s.Id)
            })
            .ToListAsync(cancellationToken);

        var members = await LoadMembersAsync(list.ActivityId, userId, cancellationToken);
        var assignedCounts = await dbContext.ActivityListItems
            .Where(i => i.ActivityListId == listId && i.AssignedToWorkgroupMemberId != null)
            .GroupBy(i => i.AssignedToWorkgroupMemberId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
        members.ForEach(m => m.AssignedCount = assignedCounts.GetValueOrDefault(m.Id));

        var schema = new ActivityListSchemaDto
        {
            ListId = list.Id,
            ShowNote = list.ShowNote,
            Statuses = statuses,
            Members = members,
            Columns = list.Columns
                .OrderBy(c => c.Kind == ActivityListColumnKind.Imported ? 0 : 1)
                .ThenBy(c => c.Order)
                .Select(c => c.ToDto())
                .ToList()
        };
        schema.SumColumnId = schema.Columns.Any(c => c.Id == list.SumColumnId && c.Kind != ActivityListColumnKind.YesNo)
            ? list.SumColumnId
            : null;

        return (list, schema, members.Where(m => m.IsMe).Select(m => m.Id).ToHashSet());
    }

    private Task<List<ActivityListMemberDto>> LoadMembersAsync(int activityId, string? userId, CancellationToken cancellationToken) =>
        dbContext.ActivityWorkgroupMembers.AsNoTracking()
            .Where(m => m.ActivityId == activityId)
            .OrderBy(m => m.Order)
            .Select(m => new ActivityListMemberDto
            {
                Id = m.Id,
                Name = m.ApplicationUser != null ? m.ApplicationUser.DisplayName : (m.Name ?? string.Empty),
                Role = m.Role,
                IsLinkedUser = m.ApplicationUserId != null,
                IsMe = userId != null && m.ApplicationUserId == userId,
                Email = m.ApplicationUserId == null ? m.Email : null,
                Mobile = m.ApplicationUserId == null ? m.Mobile : null,
                PublicId = m.PublicId
            })
            .ToListAsync(cancellationToken);

    private async Task<List<int>> GetMyMemberIdsAsync(int activityId, string? userId, CancellationToken cancellationToken)
    {
        if (userId is null)
            return [];
        return await dbContext.ActivityWorkgroupMembers
            .Where(m => m.ActivityId == activityId && m.ApplicationUserId == userId)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<ActivityListItem> BuildQuery(SearchActivityListItemsDto search, ActivityListSchemaDto schema, HashSet<int> myMemberIds)
    {
        var query = dbContext.ActivityListItems.AsNoTracking().Where(i => i.ActivityListId == search.ListId);

        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            // LIKE is case-insensitive (for ASCII) in SQLite, unlike Contains/instr.
            var pattern = "%" + search.SearchText.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            var searchNote = schema.ShowNote;
            query = query.Where(i =>
                (searchNote && i.Note != null && EF.Functions.Like(i.Note, pattern, "\\")) ||
                i.Values.Any(v => v.Value != null && EF.Functions.Like(v.Value, pattern, "\\")));
        }

        if (search.StatusId.HasValue)
            query = query.Where(i => i.StatusId == search.StatusId.Value);

        if (search.Assigned == ActivityListRules.AssignedMine)
        {
            var mine = myMemberIds.ToList();
            query = query.Where(i => i.AssignedToWorkgroupMemberId != null && mine.Contains(i.AssignedToWorkgroupMemberId.Value));
        }
        else if (search.Assigned == ActivityListRules.AssignedNone)
        {
            query = query.Where(i => i.AssignedToWorkgroupMemberId == null);
        }
        else if (int.TryParse(search.Assigned, out var memberId))
        {
            query = query.Where(i => i.AssignedToWorkgroupMemberId == memberId);
        }

        if (schema.ShowNote && search.NoteFilter == "with")
            query = query.Where(i => i.Note != null && i.Note != "");
        else if (schema.ShowNote && search.NoteFilter == "without")
            query = query.Where(i => i.Note == null || i.Note == "");

        return query;
    }

    private static IQueryable<ActivityListItem> Sort(IQueryable<ActivityListItem> query, SearchActivityListItemsDto search)
    {
        var desc = string.Equals(search.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        IOrderedQueryable<ActivityListItem> By<TKey>(Expression<Func<ActivityListItem, TKey>> key)
            => desc ? query.OrderByDescending(key) : query.OrderBy(key);

        var sortColumn = search.SortColumn ?? string.Empty;
        IOrderedQueryable<ActivityListItem> ordered;
        if (sortColumn == "status")
            ordered = By(i => i.Status == null ? int.MaxValue : i.Status.Order);
        else if (sortColumn == "assigned")
            ordered = By(i => i.AssignedToWorkgroupMember == null
                ? null
                : (i.AssignedToWorkgroupMember.ApplicationUser != null ? i.AssignedToWorkgroupMember.ApplicationUser.DisplayName : i.AssignedToWorkgroupMember.Name));
        else if (sortColumn == "note")
            ordered = By(i => i.Note);
        else if (sortColumn.StartsWith('c') && int.TryParse(sortColumn[1..], out var columnId))
            ordered = By(i => i.Values.Where(v => v.ActivityListColumnId == columnId).Select(v => v.Value).FirstOrDefault());
        else
            return desc ? query.OrderByDescending(i => i.Order) : query.OrderBy(i => i.Order);

        return ordered.ThenBy(i => i.Order);
    }

    /// <summary>Validates/normalizes a value for a column. Returns an error text, or null when the value is fine.</summary>
    private static string? NormalizeCellValue(ActivityListColumn column, ref string? value)
    {
        switch (column.Kind)
        {
            case ActivityListColumnKind.YesNo:
                value = value is "true" or "on" or "Ja" ? "true" : null;
                return null;
            case ActivityListColumnKind.Choice:
                if (value is not null && !ActivityListMapper.ParseOptions(column.Options).Contains(value))
                    return "Ugyldigt valg.";
                return null;
            default:
                if (value?.Length > ActivityListRules.MaxCellLength)
                    return $"Værdien må højst være {ActivityListRules.MaxCellLength} tegn.";
                return null;
        }
    }

    private static void SetCellValue(ActivityListItem item, int columnId, string? value)
    {
        var cell = item.Values.FirstOrDefault(v => v.ActivityListColumnId == columnId);
        if (cell is null)
        {
            if (value is not null)
                item.Values.Add(new ActivityListCellValue { ActivityListColumnId = columnId, Value = value });
        }
        else
        {
            cell.Value = value;
        }
    }

    /// <summary>True when the value, after trimming spaces, is nothing but the digits 0-9.</summary>
    private static bool TryParseWholeNumber(string value, out decimal number)
    {
        number = 0;
        var text = value.Trim();
        return text.Length > 0
            && text.All(char.IsAsciiDigit)
            && decimal.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number);
    }
}
