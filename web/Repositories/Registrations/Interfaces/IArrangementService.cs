using web.Repositories.Registrations.Dtos;
using web.ViewModels;

namespace web.Repositories.Registrations.Interfaces
{
    public interface IArrangementService
    {
        Task<ArrangementFilterViewModel> GetArrangementsAsync(ArrangementFilterViewModel filter, CancellationToken ct = default);

        /// <summary>Empty builder view model with the Adgang-tab option lists populated, for Create().</summary>
        Task<ArrangementBuilderViewModel> GetBuilderShellAsync(CancellationToken ct = default);

        Task<ArrangementBuilderViewModel?> GetArrangementForBuilderAsync(int id, CancellationToken ct = default);

        Task<SaveArrangementResponseDto> SaveArrangementAsync(SaveArrangementRequestDto dto, CancellationToken ct = default);

        Task<bool> DeleteArrangementAsync(int id, CancellationToken ct = default);

        Task<List<PersonGroupOptionViewModel>> GetGroupOptionsAsync(CancellationToken ct = default);

        Task<List<ArrangementPersonOptionViewModel>> GetPersonOptionsAsync(CancellationToken ct = default);
    }
}
