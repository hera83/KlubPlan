using web.Repositories.ActivityLists.Dtos;

namespace web.Repositories.ActivityLists.Interfaces;

/// <summary>
/// Work lists on an activity ("Lister"-fanen): columns, statuses, lines with cell values and labels,
/// worked on line by line (Status / Note / Tilknyttet / extra columns). Every method that takes an
/// item/column id also takes the listId and verifies they belong together.
/// </summary>
public interface IActivityListRepository
{
    /// <summary>The activity's lists, newest first, with counts (mine = lines assigned to the user's membership).</summary>
    Task<List<ActivityListSummaryDto>> GetSummariesAsync(int activityId, string? userId, CancellationToken cancellationToken = default);

    /// <summary>Creates the list with its imported columns, the default statuses and the lines (in batches, one transaction).</summary>
    Task<ActivityListActionResultDto> CreateAsync(CreateActivityListDto dto, CancellationToken cancellationToken = default);

    Task<ActivityListActionResultDto> UpdateAsync(int listId, string title, string? description, CancellationToken cancellationToken = default);

    /// <summary>Id in the result is the activity the list belonged to.</summary>
    Task<ActivityListActionResultDto> DeleteAsync(int listId, CancellationToken cancellationToken = default);

    /// <summary>The list page with the first page of lines. Null when not found.</summary>
    Task<ActivityListDto?> GetDetailsAsync(int listId, string? userId, CancellationToken cancellationToken = default);

    /// <summary>Counts per status/member + totals — refreshes the list page's progress bar after a change.</summary>
    Task<ActivityListCountsDto?> GetCountsAsync(int listId, CancellationToken cancellationToken = default);

    /// <summary>One page of lines matching the search (page size 1-500, otherwise 10). Null when the list doesn't exist.</summary>
    Task<ActivityListItemPageDto?> GetItemsAsync(SearchActivityListItemsDto search, string? userId, CancellationToken cancellationToken = default);

    Task<ActivityListFieldUpdateResultDto> UpdateFieldAsync(UpdateActivityListFieldDto dto, string? userId, CancellationToken cancellationToken = default);

    /// <summary>Adds (itemId null) or edits a line's column values — incl. the imported columns.</summary>
    Task<ActivityListActionResultDto> SaveItemAsync(int listId, int? itemId, IDictionary<int, string?> values, string? userId, CancellationToken cancellationToken = default);

    Task<ActivityListActionResultDto> DeleteItemAsync(int listId, int itemId, CancellationToken cancellationToken = default);

    /// <summary>Splits lines into contiguous blocks so the selected members end up with (almost) the same number of lines each.</summary>
    Task<ActivityListActionResultDto> DistributeAsync(int listId, IReadOnlyCollection<int> memberIds, bool onlyUnassigned, string? userId, CancellationToken cancellationToken = default);

    Task<ActivityListActionResultDto> SaveColumnAsync(SaveActivityListColumnDto dto, CancellationToken cancellationToken = default);

    /// <summary>Only extra columns can be deleted — imported columns and Status/Tilknyttet are fixed.</summary>
    Task<ActivityListActionResultDto> DeleteColumnAsync(int listId, int columnId, CancellationToken cancellationToken = default);

    Task<ActivityListActionResultDto> SetColumnHiddenAsync(int listId, int columnId, bool hidden, CancellationToken cancellationToken = default);

    /// <summary>Hides/shows the Note column. Existing notes are kept while hidden.</summary>
    Task<ActivityListActionResultDto> SetNoteVisibleAsync(int listId, bool visible, CancellationToken cancellationToken = default);

    /// <summary>Replaces the list's statuses. Lines with a removed status get the default status.</summary>
    Task<ActivityListActionResultDto> SaveStatusesAsync(SaveActivityListStatusesDto dto, CancellationToken cancellationToken = default);

    /// <summary>All lines matching the search in its sort order, for the Excel export. Null when the list doesn't exist.</summary>
    Task<ActivityListExportDataDto?> GetExportDataAsync(SearchActivityListItemsDto search, string? userId, CancellationToken cancellationToken = default);

    /// <summary>"Optælling": adds up the whole-number cells of one column over the lines matching the search. Null when the list/column doesn't exist.</summary>
    Task<ActivityListColumnSumDto?> SumColumnAsync(SearchActivityListItemsDto search, int columnId, string? userId, CancellationToken cancellationToken = default);

    /// <summary>Saves which column "Optælling" sums on the list (null = remove the card).</summary>
    Task<ActivityListActionResultDto> SetSumColumnAsync(int listId, int? columnId, CancellationToken cancellationToken = default);

    /// <summary>The selected external contacts (no login) in the list's arbejdsgruppe with their line counts. Null when the list doesn't exist.</summary>
    Task<ActivityListLinkRecipientsDto?> GetLinkRecipientsAsync(int listId, IReadOnlyCollection<int> memberIds, CancellationToken cancellationToken = default);

    // Public, unauthenticated /Arbejdsliste?Id={listId}&UId={member.PublicId} — an external contact
    // in the activity's workgroup sees and works on only the lines assigned to them.

    Task<ActivityListPublicDto> GetPublicListAsync(int listId, Guid memberPublicId, CancellationToken cancellationToken = default);

    /// <summary>Null when the link isn't (or no longer) valid.</summary>
    Task<ActivityListItemPageDto?> GetPublicItemsAsync(Guid memberPublicId, SearchActivityListItemsDto search, CancellationToken cancellationToken = default);

    /// <summary>Status, Note or an extra column on one of the member's own lines — never Tilknyttet.</summary>
    Task<ActivityListFieldUpdateResultDto> UpdatePublicFieldAsync(Guid memberPublicId, UpdateActivityListFieldDto dto, CancellationToken cancellationToken = default);

    // Labels on the lines.

    /// <summary>Null when the line isn't found on the list.</summary>
    Task<ActivityListItemLabelsDto?> GetItemLabelsAsync(int listId, int itemId, CancellationToken cancellationToken = default);

    /// <summary>Replaces the line's labels (already normalized: no empty rows). Saving none removes them all.</summary>
    Task<ActivityListActionResultDto> SaveItemLabelsAsync(int listId, int itemId, List<ActivityListLabelDto> labels, CancellationToken cancellationToken = default);

    /// <summary>The labels on the lines matching the search, in line order and then label order. Null when the list doesn't exist.</summary>
    Task<ActivityListPrintLabelsDto?> GetPrintLabelsAsync(SearchActivityListItemsDto search, string? userId, CancellationToken cancellationToken = default);
}
