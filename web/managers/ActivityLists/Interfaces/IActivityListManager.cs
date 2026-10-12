using web.Managers.ActivityLists.Dtos;
using web.Repositories.ActivityLists.Dtos;

namespace web.Managers.ActivityLists.Interfaces;

/// <summary>Work list operations beyond the list repository: Excel import/export and sending the personal links.</summary>
public interface IActivityListManager
{
    /// <summary>Validates and reads the uploaded .xlsx and creates the list from it.</summary>
    Task<ActivityListActionResultDto> CreateFromExcelAsync(CreateActivityListFromExcelDto dto, CancellationToken cancellationToken = default);

    /// <summary>The whole list, or only the lines matching the search, as .xlsx. Null when the list doesn't exist.</summary>
    Task<ActivityListExportDto?> ExportAsync(SearchActivityListItemsDto search, string? userId, CancellationToken cancellationToken = default);

    /// <summary>Queues an e-mail and/or SMS with each selected external contact's personal /Arbejdsliste link.</summary>
    Task<ActivityListActionResultDto> SendLinksAsync(SendActivityListLinksDto dto, string baseUrl, CancellationToken cancellationToken = default);
}
