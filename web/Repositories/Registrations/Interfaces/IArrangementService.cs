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

        Task<ToggleArrangementRegistrationResponseDto> ToggleRegistrationOpenAsync(int id, CancellationToken ct = default);

        Task<List<PersonGroupOptionViewModel>> GetGroupOptionsAsync(CancellationToken ct = default);

        Task<List<ArrangementPersonOptionViewModel>> GetPersonOptionsAsync(CancellationToken ct = default);

        /// <summary>Access gate + arrangement data for the public, unauthenticated /Tilmelding link. arrangementPublicId is Arrangement.PublicId, not Arrangement.Id.</summary>
        Task<PublicArrangementAccessViewModel> GetPublicArrangementAsync(Guid arrangementPublicId, Guid? personPublicId, CancellationToken ct = default);

        Task<SubmitPublicArrangementResponseDto> SubmitPublicArrangementAsync(SubmitPublicArrangementRequestDto dto, CancellationToken ct = default);

        Task<ArrangementRegistrationsViewModel?> GetArrangementRegistrationsAsync(int arrangementId, int page, int pageSize, CancellationToken ct = default);

        Task<ArrangementRegistrationsViewModel?> GetAllArrangementRegistrationsForExportAsync(int arrangementId, CancellationToken ct = default);
    }
}
