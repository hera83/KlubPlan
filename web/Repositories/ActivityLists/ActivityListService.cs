using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.ActivityLists.Dtos;
using web.Repositories.ActivityLists.Interfaces;
using web.ViewModels;

namespace web.Repositories.ActivityLists
{
    public class ActivityListService : IActivityListService
    {
        private const int ImportBatchSize = 500;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<ActivityListService> _logger;

        public ActivityListService(ApplicationDbContext context, ILogger<ActivityListService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ─── Lists ───────────────────────────────────────────────────────────────

        public async Task<List<ActivityListSummaryViewModel>> GetListsAsync(int activityId, string? userId, CancellationToken ct = default)
        {
            var lists = await _context.ActivityLists
                .AsNoTracking()
                .Where(l => l.ActivityId == activityId)
                .OrderByDescending(l => l.CreatedAtUtc)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.Description,
                    l.SourceFileName,
                    l.CreatedAtUtc,
                    CreatedBy = _context.Users.Where(u => u.Id == l.CreatedByUserId).Select(u => u.DisplayName).FirstOrDefault()
                })
                .ToListAsync(ct);

            if (lists.Count == 0)
                return new List<ActivityListSummaryViewModel>();

            var listIds = lists.Select(l => l.Id).ToList();
            var myMemberIds = await GetMyMemberIdsAsync(activityId, userId, ct);

            var statuses = await _context.ActivityListStatuses
                .AsNoTracking()
                .Where(s => listIds.Contains(s.ActivityListId))
                .OrderBy(s => s.Order)
                .Select(s => new
                {
                    s.ActivityListId,
                    Status = new ActivityListStatusViewModel
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Color = s.Color,
                        IsDefault = s.IsDefault,
                        Count = _context.ActivityListItems.Count(i => i.StatusId == s.Id)
                    }
                })
                .ToListAsync(ct);

            var counts = await _context.ActivityListItems
                .Where(i => listIds.Contains(i.ActivityListId))
                .GroupBy(i => i.ActivityListId)
                .Select(g => new
                {
                    ListId = g.Key,
                    Total = g.Count(),
                    Unassigned = g.Count(i => i.AssignedToWorkgroupMemberId == null),
                    Mine = g.Count(i => i.AssignedToWorkgroupMemberId != null && myMemberIds.Contains(i.AssignedToWorkgroupMemberId.Value))
                })
                .ToDictionaryAsync(c => c.ListId, ct);

            return lists.Select(l =>
            {
                counts.TryGetValue(l.Id, out var c);
                return new ActivityListSummaryViewModel
                {
                    Id = l.Id,
                    Title = l.Title,
                    Description = l.Description,
                    SourceFileName = l.SourceFileName,
                    CreatedAtUtc = l.CreatedAtUtc,
                    CreatedByName = l.CreatedBy,
                    ItemCount = c?.Total ?? 0,
                    UnassignedCount = c?.Unassigned ?? 0,
                    MineCount = c?.Mine ?? 0,
                    Statuses = statuses.Where(s => s.ActivityListId == l.Id).Select(s => s.Status).ToList()
                };
            }).ToList();
        }

        public async Task<ActivityListActionResultDto> CreateAsync(CreateActivityListRequestDto request, CancellationToken ct = default)
        {
            if (!await _context.Activities.AnyAsync(a => a.Id == request.ActivityId, ct))
                return ActivityListActionResultDto.Fail("Aktiviteten blev ikke fundet.");

            if (!string.Equals(Path.GetExtension(request.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
                return ActivityListActionResultDto.Fail("Vælg en Excel-fil i formatet .xlsx.");

            var import = ActivityListExcel.Parse(request.Content);
            if (!import.Success)
                return ActivityListActionResultDto.Fail(import.ErrorMessage!);
            if (import.Rows.Count == 0)
                return ActivityListActionResultDto.Fail("Arket har ingen linjer under overskriftsrækken.");

            var now = DateTime.UtcNow;
            var list = new ActivityList
            {
                ActivityId = request.ActivityId,
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                SourceFileName = Path.GetFileName(request.FileName),
                CreatedByUserId = request.UserId,
                CreatedAtUtc = now
            };
            var columns = import.Headers
                .Select((name, i) => new ActivityListColumn { Name = name, Kind = ActivityListColumnKind.Imported, Order = i, CreatedAtUtc = now })
                .ToList();
            var statuses = ActivityListRules.DefaultStatuses
                .Select((s, i) => new ActivityListStatus { Name = s.Name, Color = s.Color, Order = i, IsDefault = i == 0 })
                .ToList();

            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            list.Columns = columns;
            list.Statuses = statuses;
            _context.ActivityLists.Add(list);
            await _context.SaveChangesAsync(ct);

            var defaultStatusId = statuses[0].Id;
            var columnIds = columns.Select(c => c.Id).ToArray();
            var listId = list.Id;

            // Inserted in batches with a cleared change tracker — keeps memory flat for big sheets.
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            try
            {
                for (var start = 0; start < import.Rows.Count; start += ImportBatchSize)
                {
                    var batch = import.Rows.Skip(start).Take(ImportBatchSize).Select((cells, i) => new ActivityListItem
                    {
                        ActivityListId = listId,
                        Order = start + i + 1,
                        StatusId = defaultStatusId,
                        CreatedAtUtc = now,
                        Values = cells
                            .Select((value, c) => (value, c))
                            .Where(x => x.value is not null)
                            .Select(x => new ActivityListCellValue { ActivityListColumnId = columnIds[x.c], Value = x.value })
                            .ToList()
                    });
                    _context.ActivityListItems.AddRange(batch);
                    await _context.SaveChangesAsync(ct);
                    _context.ChangeTracker.Clear();
                }
            }
            finally
            {
                _context.ChangeTracker.AutoDetectChangesEnabled = true;
            }

            await tx.CommitAsync(ct);

            _logger.LogInformation("Activity {ActivityId}: created list {ListId} '{Title}' from {FileName} ({Rows} rows, {Columns} columns)",
                request.ActivityId, listId, list.Title, list.SourceFileName, import.Rows.Count, columns.Count);

            return ActivityListActionResultDto.Ok(listId, $"Listen er oprettet med {import.Rows.Count} linje{(import.Rows.Count == 1 ? "" : "r")} og {columns.Count} kolonne{(columns.Count == 1 ? "" : "r")}.");
        }

        public async Task<ActivityListActionResultDto> UpdateAsync(int listId, string title, string? description, CancellationToken ct = default)
        {
            var list = await _context.ActivityLists.FirstOrDefaultAsync(l => l.Id == listId, ct);
            if (list is null)
                return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

            list.Title = title.Trim();
            list.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            list.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return ActivityListActionResultDto.Ok(list.Id);
        }

        public async Task<ActivityListActionResultDto> DeleteAsync(int listId, CancellationToken ct = default)
        {
            var list = await _context.ActivityLists.AsNoTracking().FirstOrDefaultAsync(l => l.Id == listId, ct);
            if (list is null)
                return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

            // Columns, statuses, lines and cell values cascade in the database.
            await _context.ActivityLists.Where(l => l.Id == listId).ExecuteDeleteAsync(ct);
            _logger.LogInformation("Activity {ActivityId}: deleted list {ListId} '{Title}'", list.ActivityId, listId, list.Title);
            return ActivityListActionResultDto.Ok(list.ActivityId);
        }

        // ─── List page ───────────────────────────────────────────────────────────

        public async Task<ActivityListDetailsViewModel?> GetDetailsAsync(int listId, string? userId, CancellationToken ct = default)
        {
            var context = await LoadSchemaAsync(listId, userId, ct);
            if (context is null)
                return null;

            var (list, schema, myMemberIds) = context.Value;
            var items = await GetItemsAsync(new ActivityListItemFilterViewModel { ListId = listId }, userId, ct);

            return new ActivityListDetailsViewModel
            {
                Id = list.Id,
                ActivityId = list.ActivityId,
                ActivityTitle = list.Activity.Title,
                Title = list.Title,
                Description = list.Description,
                SourceFileName = list.SourceFileName,
                CreatedAtUtc = list.CreatedAtUtc,
                TotalCount = schema.Statuses.Sum(s => s.Count) + await _context.ActivityListItems.CountAsync(i => i.ActivityListId == listId && i.StatusId == null, ct),
                UnassignedCount = await _context.ActivityListItems.CountAsync(i => i.ActivityListId == listId && i.AssignedToWorkgroupMemberId == null, ct),
                CurrentUserIsMember = myMemberIds.Count > 0,
                Schema = schema,
                Items = items!
            };
        }

        public async Task<ActivityListCountsDto?> GetCountsAsync(int listId, CancellationToken ct = default)
        {
            if (!await _context.ActivityLists.AnyAsync(l => l.Id == listId, ct))
                return null;

            var items = _context.ActivityListItems.Where(i => i.ActivityListId == listId);
            return new ActivityListCountsDto
            {
                Total = await items.CountAsync(ct),
                Unassigned = await items.CountAsync(i => i.AssignedToWorkgroupMemberId == null, ct),
                StatusCounts = await items
                    .Where(i => i.StatusId != null)
                    .GroupBy(i => i.StatusId!.Value)
                    .Select(g => new { g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.Key, g => g.Count, ct),
                MemberCounts = await items
                    .Where(i => i.AssignedToWorkgroupMemberId != null)
                    .GroupBy(i => i.AssignedToWorkgroupMemberId!.Value)
                    .Select(g => new { g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.Key, g => g.Count, ct)
            };
        }

        public async Task<ActivityListItemsViewModel?> GetItemsAsync(ActivityListItemFilterViewModel filter, string? userId, CancellationToken ct = default)
        {
            var context = await LoadSchemaAsync(filter.ListId, userId, ct);
            if (context is null)
                return null;

            var (_, schema, myMemberIds) = context.Value;
            return await QueryItemsAsync<ActivityListItemsViewModel>(filter, schema, myMemberIds, ct);
        }

        private async Task<T> QueryItemsAsync<T>(ActivityListItemFilterViewModel filter, ActivityListSchemaViewModel schema, HashSet<int> myMemberIds, CancellationToken ct)
            where T : ActivityListItemsViewModel, new()
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = filter.PageSize is > 0 and <= 500 ? filter.PageSize : 10;

            var query = BuildQuery(filter, schema, myMemberIds);
            var total = await query.CountAsync(ct);
            if ((page - 1) * pageSize >= total && total > 0)
                page = (int)Math.Ceiling(total / (double)pageSize);

            var rows = await Sort(query, filter)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new
                {
                    i.Id,
                    i.Order,
                    i.StatusId,
                    i.Note,
                    i.AssignedToWorkgroupMemberId,
                    i.UpdatedAtUtc,
                    UpdatedBy = i.UpdatedByUser != null ? i.UpdatedByUser.DisplayName
                        : i.UpdatedByWorkgroupMember != null ? i.UpdatedByWorkgroupMember.Name : null,
                    Values = i.Values.Select(v => new { v.ActivityListColumnId, v.Value }).ToList()
                })
                .ToListAsync(ct);

            return new T
            {
                ListId = filter.ListId,
                SearchText = filter.SearchText,
                StatusId = filter.StatusId,
                Assigned = filter.Assigned,
                NoteFilter = filter.NoteFilter,
                SortColumn = filter.SortColumn,
                SortDir = filter.SortDir,
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Schema = schema,
                Rows = rows.Select(r => new ActivityListItemRowViewModel
                {
                    Id = r.Id,
                    RowNumber = r.Order,
                    StatusId = r.StatusId,
                    Note = r.Note,
                    AssignedMemberId = r.AssignedToWorkgroupMemberId,
                    UpdatedAtUtc = r.UpdatedAtUtc,
                    UpdatedByName = r.UpdatedBy,
                    Values = r.Values.ToDictionary(v => v.ActivityListColumnId, v => v.Value)
                }).ToList()
            };
        }

        // ─── Lines ───────────────────────────────────────────────────────────────

        public Task<ActivityListFieldUpdateResultDto> UpdateFieldAsync(ActivityListFieldUpdateViewModel input, string? userId, CancellationToken ct = default)
            => UpdateFieldCoreAsync(input, userId, null, ct);

        /// <summary>
        /// Shared by the logged-in list page and the public /Arbejdsliste link. With actingMember set
        /// (an external contact), only lines assigned to that member can be changed, Tilknyttet can't be
        /// changed, and the returned counts cover the member's own lines only.
        /// </summary>
        private async Task<ActivityListFieldUpdateResultDto> UpdateFieldCoreAsync(ActivityListFieldUpdateViewModel input, string? userId, ActivityWorkgroupMember? actingMember, CancellationToken ct)
        {
            ActivityListFieldUpdateResultDto Fail(string message) => new() { Success = false, ErrorMessage = message };

            var list = await _context.ActivityLists.AsNoTracking().FirstOrDefaultAsync(l => l.Id == input.ListId, ct);
            if (list is null)
                return Fail("Listen blev ikke fundet.");

            var item = await _context.ActivityListItems
                .Include(i => i.Values)
                .FirstOrDefaultAsync(i => i.Id == input.ItemId && i.ActivityListId == input.ListId, ct);
            if (item is null)
                return Fail("Linjen blev ikke fundet — den kan være slettet.");

            if (actingMember is not null)
            {
                if (item.AssignedToWorkgroupMemberId != actingMember.Id)
                    return Fail("Linjen er ikke længere tildelt dig.");
                if (input.Field == ActivityListRules.FieldAssigned)
                    return Fail("Du kan ikke ændre, hvem linjen er tilknyttet.");
            }

            var value = string.IsNullOrWhiteSpace(input.Value) ? null : input.Value.Trim();

            switch (input.Field)
            {
                case ActivityListRules.FieldStatus:
                    if (!int.TryParse(value, out var statusId) || !await _context.ActivityListStatuses.AnyAsync(s => s.Id == statusId && s.ActivityListId == list.Id, ct))
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
                        if (!int.TryParse(value, out var memberId) || !await _context.ActivityWorkgroupMembers.AnyAsync(m => m.Id == memberId && m.ActivityId == list.ActivityId, ct))
                            return Fail("Personen er ikke i aktivitetens arbejdsgruppe.");
                        item.AssignedToWorkgroupMemberId = memberId;
                    }
                    break;

                case ActivityListRules.FieldColumn:
                    var column = input.ColumnId.HasValue
                        ? await _context.ActivityListColumns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == input.ColumnId.Value && c.ActivityListId == list.Id, ct)
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

            item.UpdatedAtUtc = DateTime.UtcNow;
            item.UpdatedByUserId = actingMember is null ? userId : null;
            item.UpdatedByWorkgroupMemberId = actingMember?.Id;
            await _context.SaveChangesAsync(ct);

            var actingMemberId = actingMember?.Id;
            var statusCounts = await _context.ActivityListItems
                .Where(i => i.ActivityListId == list.Id && i.StatusId != null && (actingMemberId == null || i.AssignedToWorkgroupMemberId == actingMemberId))
                .GroupBy(i => i.StatusId!.Value)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count, ct);

            var userName = actingMember is not null
                ? actingMember.Name
                : userId is null ? null : await _context.Users.Where(u => u.Id == userId).Select(u => u.DisplayName).FirstOrDefaultAsync(ct);

            return new ActivityListFieldUpdateResultDto
            {
                Success = true,
                StatusCounts = statusCounts,
                UnassignedCount = await _context.ActivityListItems.CountAsync(i => i.ActivityListId == list.Id && i.AssignedToWorkgroupMemberId == null, ct),
                UpdatedText = UpdatedText(userName, item.UpdatedAtUtc)
            };
        }

        public async Task<ActivityListActionResultDto> SaveItemAsync(int listId, int? itemId, IDictionary<int, string?> values, string? userId, CancellationToken ct = default)
        {
            var list = await _context.ActivityLists
                .Include(l => l.Columns)
                .Include(l => l.Statuses)
                .FirstOrDefaultAsync(l => l.Id == listId, ct);
            if (list is null)
                return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

            ActivityListItem? item;
            var isNew = itemId is null or 0;
            if (isNew)
            {
                var maxOrder = await _context.ActivityListItems.Where(i => i.ActivityListId == listId).MaxAsync(i => (int?)i.Order, ct) ?? 0;
                item = new ActivityListItem
                {
                    ActivityListId = listId,
                    Order = maxOrder + 1,
                    StatusId = list.Statuses.FirstOrDefault(s => s.IsDefault)?.Id ?? list.Statuses.OrderBy(s => s.Order).FirstOrDefault()?.Id,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _context.ActivityListItems.Add(item);
            }
            else
            {
                item = await _context.ActivityListItems.Include(i => i.Values).FirstOrDefaultAsync(i => i.Id == itemId && i.ActivityListId == listId, ct);
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

            item.UpdatedAtUtc = DateTime.UtcNow;
            item.UpdatedByUserId = userId;
            item.UpdatedByWorkgroupMemberId = null;
            await _context.SaveChangesAsync(ct);
            return ActivityListActionResultDto.Ok(item.Id, isNew ? $"Linje {item.Order} er tilføjet." : $"Linje {item.Order} er gemt.");
        }

        public async Task<ActivityListActionResultDto> DeleteItemAsync(int listId, int itemId, CancellationToken ct = default)
        {
            var deleted = await _context.ActivityListItems.Where(i => i.Id == itemId && i.ActivityListId == listId).ExecuteDeleteAsync(ct);
            return deleted > 0 ? ActivityListActionResultDto.Ok(itemId) : ActivityListActionResultDto.Fail("Linjen blev ikke fundet.");
        }

        public async Task<ActivityListActionResultDto> DistributeAsync(int listId, IReadOnlyCollection<int> memberIds, bool onlyUnassigned, string? userId, CancellationToken ct = default)
        {
            var list = await _context.ActivityLists.AsNoTracking().FirstOrDefaultAsync(l => l.Id == listId, ct);
            if (list is null)
                return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

            var members = await LoadMembersAsync(list.ActivityId, null, ct);
            members = members.Where(m => memberIds.Contains(m.Id)).ToList();
            if (members.Count == 0)
                return ActivityListActionResultDto.Fail("Vælg mindst én person fra arbejdsgruppen.");

            var items = await _context.ActivityListItems
                .Where(i => i.ActivityListId == listId && (!onlyUnassigned || i.AssignedToWorkgroupMemberId == null))
                .OrderBy(i => i.Order)
                .ToListAsync(ct);
            if (items.Count == 0)
                return ActivityListActionResultDto.Fail(onlyUnassigned ? "Alle linjer har allerede en tilknytning." : "Listen har ingen linjer.");

            // Existing lines count when only the unassigned are distributed, so the totals end up even —
            // e.g. a helper added later gets more of the remaining lines than those who already have some.
            var existing = new int[members.Count];
            if (onlyUnassigned)
            {
                var ids = members.Select(m => m.Id).ToList();
                var counts = await _context.ActivityListItems
                    .Where(i => i.ActivityListId == listId && i.AssignedToWorkgroupMemberId != null && ids.Contains(i.AssignedToWorkgroupMemberId.Value))
                    .GroupBy(i => i.AssignedToWorkgroupMemberId!.Value)
                    .Select(g => new { g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.Key, g => g.Count, ct);
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
            var now = DateTime.UtcNow;
            var index = 0;
            for (var m = 0; m < members.Count; m++)
            {
                for (var q = 0; q < quota[m]; q++, index++)
                {
                    items[index].AssignedToWorkgroupMemberId = members[m].Id;
                    items[index].UpdatedAtUtc = now;
                    items[index].UpdatedByUserId = userId;
                    items[index].UpdatedByWorkgroupMemberId = null;
                }
            }

            await _context.SaveChangesAsync(ct);

            var summary = string.Join(", ", members.Select((m, i) => $"{m.Name} {quota[i]}"));
            _logger.LogInformation("List {ListId}: distributed {Count} line(s) between {Members} member(s)", listId, items.Count, members.Count);
            return ActivityListActionResultDto.Ok(listId, $"{items.Count} linje{(items.Count == 1 ? "" : "r")} fordelt: {summary}.");
        }

        // ─── Columns & statuses ──────────────────────────────────────────────────

        public async Task<ActivityListActionResultDto> SaveColumnAsync(ActivityListColumnSaveViewModel input, CancellationToken ct = default)
        {
            var list = await _context.ActivityLists.Include(l => l.Columns).FirstOrDefaultAsync(l => l.Id == input.ListId, ct);
            if (list is null)
                return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

            var name = input.Name.Trim();
            if (name.Length == 0)
                return ActivityListActionResultDto.Fail("Angiv et kolonnenavn.");
            if (list.Columns.Any(c => c.Id != input.ColumnId && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
                return ActivityListActionResultDto.Fail($"Der findes allerede en kolonne med navnet \"{name}\".");

            var column = input.ColumnId.HasValue ? list.Columns.FirstOrDefault(c => c.Id == input.ColumnId.Value) : null;
            if (input.ColumnId.HasValue && column is null)
                return ActivityListActionResultDto.Fail("Kolonnen blev ikke fundet.");

            var kind = column?.Kind ?? input.Kind;
            if (column is null && kind == ActivityListColumnKind.Imported)
                return ActivityListActionResultDto.Fail("Vælg en kolonnetype.");

            string? options = null;
            if (kind == ActivityListColumnKind.Choice)
            {
                var parsed = ParseOptions(input.Options);
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
                    Order = list.Columns.Count == 0 ? 0 : list.Columns.Max(c => c.Order) + 1,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _context.ActivityListColumns.Add(column);
            }
            else
            {
                column.Name = name;
                if (kind == ActivityListColumnKind.Choice)
                    column.Options = options;
            }

            list.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return ActivityListActionResultDto.Ok(column.Id, input.ColumnId.HasValue ? "Kolonnen er gemt." : $"Kolonnen \"{name}\" er tilføjet.");
        }

        public async Task<ActivityListActionResultDto> DeleteColumnAsync(int listId, int columnId, CancellationToken ct = default)
        {
            var column = await _context.ActivityListColumns.FirstOrDefaultAsync(c => c.Id == columnId && c.ActivityListId == listId, ct);
            if (column is null)
                return ActivityListActionResultDto.Fail("Kolonnen blev ikke fundet.");
            if (column.Kind == ActivityListColumnKind.Imported)
                return ActivityListActionResultDto.Fail("Kolonner fra Excel-arket kan ikke slettes.");

            _context.ActivityListColumns.Remove(column);
            await _context.SaveChangesAsync(ct);
            return ActivityListActionResultDto.Ok(columnId, $"Kolonnen \"{column.Name}\" er slettet.");
        }

        public async Task<ActivityListActionResultDto> SetColumnHiddenAsync(int listId, int columnId, bool hidden, CancellationToken ct = default)
        {
            var column = await _context.ActivityListColumns.FirstOrDefaultAsync(c => c.Id == columnId && c.ActivityListId == listId, ct);
            if (column is null)
                return ActivityListActionResultDto.Fail("Kolonnen blev ikke fundet.");

            column.IsHidden = hidden;
            await _context.SaveChangesAsync(ct);
            return ActivityListActionResultDto.Ok(columnId, hidden
                ? $"Kolonnen \"{column.Name}\" er skjult i tabellen. Den kommer stadig med ved eksport."
                : $"Kolonnen \"{column.Name}\" vises igen i tabellen.");
        }

        public async Task<ActivityListActionResultDto> SetNoteVisibleAsync(int listId, bool visible, CancellationToken ct = default)
        {
            var list = await _context.ActivityLists.FirstOrDefaultAsync(l => l.Id == listId, ct);
            if (list is null)
                return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

            list.ShowNote = visible;
            list.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return ActivityListActionResultDto.Ok(listId, visible ? "Note-kolonnen er slået til." : "Note-kolonnen er fjernet. Eksisterende noter gemmes, hvis den slås til igen.");
        }

        public async Task<ActivityListActionResultDto> SaveStatusesAsync(ActivityListStatusesSaveViewModel input, CancellationToken ct = default)
        {
            var list = await _context.ActivityLists.Include(l => l.Statuses).FirstOrDefaultAsync(l => l.Id == input.ListId, ct);
            if (list is null)
                return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

            var rows = input.Statuses.Select(s => new { s.Id, Name = s.Name?.Trim() ?? string.Empty, s.Color }).ToList();
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

            var defaultIndex = Math.Clamp(input.DefaultIndex, 0, rows.Count - 1);
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

            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            await _context.SaveChangesAsync(ct);

            var removed = list.Statuses.Except(kept).ToList();
            if (removed.Count > 0)
            {
                var removedIds = removed.Select(s => s.Id).ToList();
                var defaultId = kept[defaultIndex].Id;
                await _context.ActivityListItems
                    .Where(i => i.ActivityListId == list.Id && i.StatusId != null && removedIds.Contains(i.StatusId.Value))
                    .ExecuteUpdateAsync(s => s.SetProperty(i => i.StatusId, defaultId), ct);
                _context.ActivityListStatuses.RemoveRange(removed);
                await _context.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return ActivityListActionResultDto.Ok(list.Id, "Statusserne er gemt.");
        }

        // ─── Export ──────────────────────────────────────────────────────────────

        public async Task<ActivityListExportDto?> ExportAsync(ActivityListItemFilterViewModel filter, string? userId, CancellationToken ct = default)
        {
            var context = await LoadSchemaAsync(filter.ListId, userId, ct);
            if (context is null)
                return null;

            var (list, schema, myMemberIds) = context.Value;
            var statuses = schema.Statuses.ToDictionary(s => s.Id);
            var members = schema.Members.ToDictionary(m => m.Id, m => m.Name);

            var rows = await Sort(BuildQuery(filter, schema, myMemberIds), filter)
                .Select(i => new
                {
                    i.Order,
                    i.StatusId,
                    i.Note,
                    i.AssignedToWorkgroupMemberId,
                    i.UpdatedAtUtc,
                    UpdatedBy = i.UpdatedByUser != null ? i.UpdatedByUser.DisplayName
                        : i.UpdatedByWorkgroupMember != null ? i.UpdatedByWorkgroupMember.Name : null,
                    Values = i.Values.Select(v => new { v.ActivityListColumnId, v.Value }).ToList()
                })
                .ToListAsync(ct);

            var headers = new List<string> { "Nr." };
            headers.AddRange(schema.Columns.Select(c => c.Name));
            var statusColumn = headers.Count;
            headers.Add("Status");
            headers.Add("Tilknyttet");
            if (schema.ShowNote)
                headers.Add("Note");
            headers.Add("Sidst ændret");
            headers.Add("Ændret af");

            var lines = rows.Select(r =>
            {
                var values = r.Values.ToDictionary(v => v.ActivityListColumnId, v => v.Value);
                var status = r.StatusId.HasValue ? statuses.GetValueOrDefault(r.StatusId.Value) : null;
                var cells = new List<string?> { r.Order.ToString(CultureInfo.InvariantCulture) };
                cells.AddRange(schema.Columns.Select(c =>
                {
                    var value = values.GetValueOrDefault(c.Id);
                    return c.Kind == ActivityListColumnKind.YesNo ? (value == "true" ? "Ja" : "Nej") : value;
                }));
                cells.Add(status?.Name);
                cells.Add(r.AssignedToWorkgroupMemberId.HasValue ? members.GetValueOrDefault(r.AssignedToWorkgroupMemberId.Value) : null);
                if (schema.ShowNote)
                    cells.Add(r.Note);
                cells.Add(r.UpdatedAtUtc?.ToLocalTime().ToDanishDateTime());
                cells.Add(r.UpdatedBy);
                return (cells.ToArray(), status?.Color);
            });

            var content = ActivityListExcel.Build(list.Title, headers, lines, statusColumn);

            // A per-person export gets the name in the file name, so it can be sent straight on.
            var suffix = filter.Assigned switch
            {
                ActivityListRules.AssignedMine => " - mine",
                ActivityListRules.AssignedNone => " - uden tilknytning",
                { } id when int.TryParse(id, out var memberId) && members.TryGetValue(memberId, out var name) => $" - {name}",
                _ => filter.HasFilter ? " - filtreret" : string.Empty
            };

            return new ActivityListExportDto
            {
                Content = content,
                FileName = SanitizeFileName($"{list.Title}{suffix}") + ".xlsx"
            };
        }

        // ─── Offentligt link (/Arbejdsliste) ─────────────────────────────────────

        public async Task<PublicWorkListViewModel> GetPublicListAsync(int listId, Guid memberPublicId, CancellationToken ct = default)
        {
            var resolved = await ResolvePublicAsync(listId, memberPublicId, ct);
            if (resolved is null)
                return new PublicWorkListViewModel { Found = false };

            var (list, member, schema) = resolved.Value;
            var items = await QueryPublicItemsAsync(new ActivityListItemFilterViewModel { ListId = listId }, member, schema, ct);

            return new PublicWorkListViewModel
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

        public async Task<PublicWorkListItemsViewModel?> GetPublicItemsAsync(Guid memberPublicId, ActivityListItemFilterViewModel filter, CancellationToken ct = default)
        {
            var resolved = await ResolvePublicAsync(filter.ListId, memberPublicId, ct);
            if (resolved is null)
                return null;

            var (_, member, schema) = resolved.Value;
            return await QueryPublicItemsAsync(filter, member, schema, ct);
        }

        public async Task<ActivityListFieldUpdateResultDto> UpdatePublicFieldAsync(Guid memberPublicId, ActivityListFieldUpdateViewModel input, CancellationToken ct = default)
        {
            var member = await FindPublicMemberAsync(input.ListId, memberPublicId, ct);
            if (member is null)
                return new ActivityListFieldUpdateResultDto { Success = false, ErrorMessage = "Linket er ikke længere gyldigt." };

            return await UpdateFieldCoreAsync(input, null, member, ct);
        }

        public async Task<ActivityListActionResultDto> SendLinksAsync(ActivityListSendLinksViewModel input, string baseUrl, CancellationToken ct = default)
        {
            if (!input.ViaEmail && !input.ViaSms)
                return ActivityListActionResultDto.Fail("Vælg e-mail og/eller SMS.");

            var list = await _context.ActivityLists.AsNoTracking().Include(l => l.Activity).FirstOrDefaultAsync(l => l.Id == input.ListId, ct);
            if (list is null)
                return ActivityListActionResultDto.Fail("Listen blev ikke fundet.");

            // Only external contacts — administrators in the workgroup log in and use the list page.
            var members = await _context.ActivityWorkgroupMembers
                .AsNoTracking()
                .Where(m => m.ActivityId == list.ActivityId && m.ApplicationUserId == null && input.MemberIds.Contains(m.Id))
                .OrderBy(m => m.Order)
                .ToListAsync(ct);
            if (members.Count == 0)
                return ActivityListActionResultDto.Fail("Vælg mindst én ekstern kontakt fra arbejdsgruppen.");

            var assignedCounts = await _context.ActivityListItems
                .Where(i => i.ActivityListId == list.Id && i.AssignedToWorkgroupMemberId != null)
                .GroupBy(i => i.AssignedToWorkgroupMemberId!.Value)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count, ct);

            var message = string.IsNullOrWhiteSpace(input.Message) ? null : input.Message.Trim();
            var subject = $"{list.Activity.Title}: {list.Title}";
            int emails = 0, sms = 0;
            var missing = new List<string>();

            foreach (var member in members)
            {
                var name = member.Name ?? string.Empty;
                var count = assignedCounts.GetValueOrDefault(member.Id);
                var body = $"Hej {name}\n\n"
                    + (message is null ? string.Empty : $"{message}\n\n")
                    + $"Du har {count} linje{(count == 1 ? "" : "r")} på listen \"{list.Title}\" ({list.Activity.Title}). "
                    + $"Åbn dine linjer her:\n{BuildPublicLink(baseUrl, list.Id, member.PublicId)}";

                var sent = false;
                if (input.ViaEmail && !string.IsNullOrWhiteSpace(member.Email))
                {
                    _context.CommunicationEmailMessages.Add(new CommunicationEmailMessage
                    {
                        ToAddress = member.Email.Trim(),
                        Subject = subject,
                        Body = body,
                        HtmlBody = EmailHtmlBuilder.FromPlainText(body),
                        Status = CommunicationEmailMessageStatus.Pending
                    });
                    emails++;
                    sent = true;
                }
                if (input.ViaSms && !string.IsNullOrWhiteSpace(member.Mobile))
                {
                    _context.SmsMessages.Add(new SmsMessage
                    {
                        Direction = SmsDirection.Outbound,
                        PhoneNumber = member.Mobile.Trim(),
                        Body = body,
                        Status = SmsMessageStatus.Pending
                    });
                    sms++;
                    sent = true;
                }
                if (!sent)
                    missing.Add(name);
            }

            if (emails + sms == 0)
                return ActivityListActionResultDto.Fail($"Ingen af de valgte har {(input.ViaEmail && input.ViaSms ? "e-mail eller mobilnummer" : input.ViaEmail ? "en e-mail" : "et mobilnummer")} registreret.");

            // Picked up and sent by CommunicationEmailWorker / SmsWorker.
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("List {ListId}: queued work list links to {Members} member(s) ({Emails} e-mail, {Sms} SMS)", list.Id, members.Count - missing.Count, emails, sms);

            var parts = new List<string>();
            if (emails > 0) parts.Add($"{emails} e-mail{(emails == 1 ? "" : "s")}");
            if (sms > 0) parts.Add($"{sms} SMS");
            var text = $"Links sendes: {string.Join(" og ", parts)}.";
            if (missing.Count > 0)
                text += $" Mangler kontaktinfo: {string.Join(", ", missing)}.";
            return ActivityListActionResultDto.Ok(list.Id, text);
        }

        public static string BuildPublicLink(string baseUrl, int listId, Guid memberPublicId)
            => $"{baseUrl.TrimEnd('/')}/Arbejdsliste?Id={listId}&UId={memberPublicId}";

        /// <summary>An external contact (no login) in the list's activity workgroup, or null.</summary>
        private async Task<ActivityWorkgroupMember?> FindPublicMemberAsync(int listId, Guid memberPublicId, CancellationToken ct)
        {
            if (memberPublicId == Guid.Empty)
                return null;

            return await _context.ActivityWorkgroupMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.PublicId == memberPublicId
                    && m.ApplicationUserId == null
                    && _context.ActivityLists.Any(l => l.Id == listId && l.ActivityId == m.ActivityId), ct);
        }

        /// <summary>
        /// Gate for the public link + a schema trimmed for it: no other members (their names aren't
        /// shown) and status counts for the member's own lines only.
        /// </summary>
        private async Task<(ActivityList List, ActivityWorkgroupMember Member, ActivityListSchemaViewModel Schema)?> ResolvePublicAsync(int listId, Guid memberPublicId, CancellationToken ct)
        {
            var member = await FindPublicMemberAsync(listId, memberPublicId, ct);
            if (member is null)
                return null;

            var context = await LoadSchemaAsync(listId, null, ct);
            if (context is null)
                return null;

            var (list, schema, _) = context.Value;
            var ownCounts = await _context.ActivityListItems
                .Where(i => i.ActivityListId == listId && i.AssignedToWorkgroupMemberId == member.Id && i.StatusId != null)
                .GroupBy(i => i.StatusId!.Value)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count, ct);
            schema.Statuses.ForEach(s => s.Count = ownCounts.GetValueOrDefault(s.Id));
            schema.Members = new List<ActivityListMemberViewModel>();

            return (list, member, schema);
        }

        private async Task<PublicWorkListItemsViewModel> QueryPublicItemsAsync(ActivityListItemFilterViewModel filter, ActivityWorkgroupMember member, ActivityListSchemaViewModel schema, CancellationToken ct)
        {
            // Always locked to the member's own lines, whatever the query string says.
            filter.Assigned = member.Id.ToString(CultureInfo.InvariantCulture);
            var items = await QueryItemsAsync<PublicWorkListItemsViewModel>(filter, schema, new HashSet<int>(), ct);
            items.Assigned = null;
            items.MemberPublicId = member.PublicId;
            return items;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private async Task<(ActivityList List, ActivityListSchemaViewModel Schema, HashSet<int> MyMemberIds)?> LoadSchemaAsync(int listId, string? userId, CancellationToken ct)
        {
            var list = await _context.ActivityLists
                .AsNoTracking()
                .Include(l => l.Activity)
                .Include(l => l.Columns)
                .FirstOrDefaultAsync(l => l.Id == listId, ct);
            if (list is null)
                return null;

            var statuses = await _context.ActivityListStatuses
                .AsNoTracking()
                .Where(s => s.ActivityListId == listId)
                .OrderBy(s => s.Order)
                .Select(s => new ActivityListStatusViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Color = s.Color,
                    IsDefault = s.IsDefault,
                    Count = _context.ActivityListItems.Count(i => i.StatusId == s.Id)
                })
                .ToListAsync(ct);

            var members = await LoadMembersAsync(list.ActivityId, userId, ct);
            var assignedCounts = await _context.ActivityListItems
                .Where(i => i.ActivityListId == listId && i.AssignedToWorkgroupMemberId != null)
                .GroupBy(i => i.AssignedToWorkgroupMemberId!.Value)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count, ct);
            members.ForEach(m => m.AssignedCount = assignedCounts.GetValueOrDefault(m.Id));

            var schema = new ActivityListSchemaViewModel
            {
                ListId = list.Id,
                ShowNote = list.ShowNote,
                Statuses = statuses,
                Members = members,
                Columns = list.Columns
                    .OrderBy(c => c.Kind == ActivityListColumnKind.Imported ? 0 : 1)
                    .ThenBy(c => c.Order)
                    .Select(c => new ActivityListColumnViewModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Kind = c.Kind,
                        Options = ParseOptions(c.Options),
                        IsHidden = c.IsHidden
                    })
                    .ToList()
            };

            return (list, schema, members.Where(m => m.IsMe).Select(m => m.Id).ToHashSet());
        }

        private async Task<List<ActivityListMemberViewModel>> LoadMembersAsync(int activityId, string? userId, CancellationToken ct)
        {
            return await _context.ActivityWorkgroupMembers
                .AsNoTracking()
                .Where(m => m.ActivityId == activityId)
                .OrderBy(m => m.Order)
                .Select(m => new ActivityListMemberViewModel
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
                .ToListAsync(ct);
        }

        private async Task<List<int>> GetMyMemberIdsAsync(int activityId, string? userId, CancellationToken ct)
        {
            if (userId is null)
                return new List<int>();
            return await _context.ActivityWorkgroupMembers
                .Where(m => m.ActivityId == activityId && m.ApplicationUserId == userId)
                .Select(m => m.Id)
                .ToListAsync(ct);
        }

        private IQueryable<ActivityListItem> BuildQuery(ActivityListItemFilterViewModel filter, ActivityListSchemaViewModel schema, HashSet<int> myMemberIds)
        {
            var query = _context.ActivityListItems.AsNoTracking().Where(i => i.ActivityListId == filter.ListId);

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                // LIKE is case-insensitive (for ASCII) in SQLite, unlike Contains/instr.
                var pattern = "%" + filter.SearchText.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
                var searchNote = schema.ShowNote;
                query = query.Where(i =>
                    (searchNote && i.Note != null && EF.Functions.Like(i.Note, pattern, "\\")) ||
                    i.Values.Any(v => v.Value != null && EF.Functions.Like(v.Value, pattern, "\\")));
            }

            if (filter.StatusId.HasValue)
                query = query.Where(i => i.StatusId == filter.StatusId.Value);

            if (filter.Assigned == ActivityListRules.AssignedMine)
            {
                var mine = myMemberIds.ToList();
                query = query.Where(i => i.AssignedToWorkgroupMemberId != null && mine.Contains(i.AssignedToWorkgroupMemberId.Value));
            }
            else if (filter.Assigned == ActivityListRules.AssignedNone)
            {
                query = query.Where(i => i.AssignedToWorkgroupMemberId == null);
            }
            else if (int.TryParse(filter.Assigned, out var memberId))
            {
                query = query.Where(i => i.AssignedToWorkgroupMemberId == memberId);
            }

            if (schema.ShowNote && filter.NoteFilter == "with")
                query = query.Where(i => i.Note != null && i.Note != "");
            else if (schema.ShowNote && filter.NoteFilter == "without")
                query = query.Where(i => i.Note == null || i.Note == "");

            return query;
        }

        private static IQueryable<ActivityListItem> Sort(IQueryable<ActivityListItem> query, ActivityListItemFilterViewModel filter)
        {
            var desc = string.Equals(filter.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

            IOrderedQueryable<ActivityListItem> By<TKey>(Expression<Func<ActivityListItem, TKey>> key)
                => desc ? query.OrderByDescending(key) : query.OrderBy(key);

            var sortColumn = filter.SortColumn ?? string.Empty;
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
                    if (value is not null && !ParseOptions(column.Options).Contains(value))
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

        private static List<string> ParseOptions(string? options)
            => (options ?? string.Empty)
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(50)
                .ToList();

        public static string? UpdatedText(string? userName, DateTime? updatedAtUtc)
        {
            if (!updatedAtUtc.HasValue)
                return null;
            var when = updatedAtUtc.Value.ToLocalTime().ToDanishDateTime();
            return string.IsNullOrEmpty(userName) ? $"Ændret {when}" : $"Ændret af {userName} · {when}";
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars().Concat(new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' }).ToHashSet();
            var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
            return cleaned.Length == 0 ? "liste" : cleaned;
        }
    }
}
