using web.Repositories.Logs.Dtos;

namespace web.ViewModels.Mappers;

/// <summary>Maps between the Settings → Logs view models and the Logs repository's Dtos.</summary>
public static class LogsMapper
{
    public static SearchLogsDto ToSearchLogsDto(this LogFilterViewModel filter) => new()
    {
        Level = filter.Level,
        SearchText = filter.SearchText,
        FromUtc = filter.FromUtc,
        ToUtc = filter.ToUtc,
        Page = filter.Page,
        PageSize = filter.PageSize
    };

    public static LogFilterViewModel ToLogFilterViewModel(this LogSearchResultDto result, LogFilterViewModel filter) => new()
    {
        Level = filter.Level,
        SearchText = filter.SearchText,
        FromUtc = filter.FromUtc,
        ToUtc = filter.ToUtc,
        Page = result.Page,
        PageSize = result.PageSize,
        TotalCount = result.TotalCount,
        Entries = result.Items.Select(ToLogEntryViewModel).ToList()
    };

    public static LogEntryViewModel ToLogEntryViewModel(this LogEntryDto entry) => new()
    {
        Id = entry.Id,
        TimestampUtc = entry.TimestampUtc,
        Level = entry.Level,
        Message = entry.Message,
        Exception = entry.Exception,
        Properties = entry.Properties
    };
}
