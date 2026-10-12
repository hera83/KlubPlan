using web.Repositories.Logs.Dtos;

namespace web.Repositories.Logs.Interfaces;

public interface ILogRepository
{
    Task<LogSearchResultDto> SearchAsync(SearchLogsDto search, CancellationToken cancellationToken = default);
}
