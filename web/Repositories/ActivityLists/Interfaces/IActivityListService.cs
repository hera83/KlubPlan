using web.Repositories.ActivityLists.Dtos;
using web.ViewModels;

namespace web.Repositories.ActivityLists.Interfaces
{
    /// <summary>
    /// Work lists on an activity ("Lister"-fanen): created from an Excel sheet, worked on line by line
    /// (Status / Note / Tilknyttet / extra columns) and exported back to Excel. Every method that
    /// takes an item/column id also takes the listId and verifies they belong together.
    /// </summary>
    public interface IActivityListService
    {
        Task<List<ActivityListSummaryViewModel>> GetListsAsync(int activityId, string? userId, CancellationToken ct = default);

        Task<ActivityListActionResultDto> CreateAsync(CreateActivityListRequestDto request, CancellationToken ct = default);

        Task<ActivityListActionResultDto> UpdateAsync(int listId, string title, string? description, CancellationToken ct = default);

        /// <summary>Id in the result is the activity the list belonged to.</summary>
        Task<ActivityListActionResultDto> DeleteAsync(int listId, CancellationToken ct = default);

        Task<ActivityListDetailsViewModel?> GetDetailsAsync(int listId, string? userId, CancellationToken ct = default);

        /// <summary>Counts per status/member + totals — refreshes the list page's progress bar after a change.</summary>
        Task<ActivityListCountsDto?> GetCountsAsync(int listId, CancellationToken ct = default);

        Task<ActivityListItemsViewModel?> GetItemsAsync(ActivityListItemFilterViewModel filter, string? userId, CancellationToken ct = default);

        Task<ActivityListFieldUpdateResultDto> UpdateFieldAsync(ActivityListFieldUpdateViewModel input, string? userId, CancellationToken ct = default);

        /// <summary>Adds (itemId null) or edits a line's column values — incl. the imported columns.</summary>
        Task<ActivityListActionResultDto> SaveItemAsync(int listId, int? itemId, IDictionary<int, string?> values, string? userId, CancellationToken ct = default);

        Task<ActivityListActionResultDto> DeleteItemAsync(int listId, int itemId, CancellationToken ct = default);

        /// <summary>Splits lines into contiguous blocks so the selected members end up with (almost) the same number of lines each.</summary>
        Task<ActivityListActionResultDto> DistributeAsync(int listId, IReadOnlyCollection<int> memberIds, bool onlyUnassigned, string? userId, CancellationToken ct = default);

        Task<ActivityListActionResultDto> SaveColumnAsync(ActivityListColumnSaveViewModel input, CancellationToken ct = default);

        /// <summary>Only extra columns can be deleted — imported columns and Status/Tilknyttet are fixed.</summary>
        Task<ActivityListActionResultDto> DeleteColumnAsync(int listId, int columnId, CancellationToken ct = default);

        /// <summary>Hides/shows the Note column. Existing notes are kept while hidden.</summary>
        Task<ActivityListActionResultDto> SetColumnHiddenAsync(int listId, int columnId, bool hidden, CancellationToken ct = default);

        Task<ActivityListActionResultDto> SetNoteVisibleAsync(int listId, bool visible, CancellationToken ct = default);

        /// <summary>Replaces the list's statuses. Lines with a removed status get the default status.</summary>
        Task<ActivityListActionResultDto> SaveStatusesAsync(ActivityListStatusesSaveViewModel input, CancellationToken ct = default);

        /// <summary>The whole list, or only the lines matching the filter, as .xlsx.</summary>
        Task<ActivityListExportDto?> ExportAsync(ActivityListItemFilterViewModel filter, string? userId, CancellationToken ct = default);

        /// <summary>Queues an e-mail and/or SMS with each selected external contact's personal /Arbejdsliste link.</summary>
        Task<ActivityListActionResultDto> SendLinksAsync(ActivityListSendLinksViewModel input, string baseUrl, CancellationToken ct = default);

        // Public, unauthenticated /Arbejdsliste?Id={listId}&UId={member.PublicId} — an external contact
        // in the activity's workgroup sees and works on only the lines assigned to them.

        Task<PublicWorkListViewModel> GetPublicListAsync(int listId, Guid memberPublicId, CancellationToken ct = default);

        /// <summary>Null when the link isn't (or no longer) valid.</summary>
        Task<PublicWorkListItemsViewModel?> GetPublicItemsAsync(Guid memberPublicId, ActivityListItemFilterViewModel filter, CancellationToken ct = default);

        /// <summary>Status, Note or an extra column on one of the member's own lines — never Tilknyttet.</summary>
        Task<ActivityListFieldUpdateResultDto> UpdatePublicFieldAsync(Guid memberPublicId, ActivityListFieldUpdateViewModel input, CancellationToken ct = default);
    }
}
