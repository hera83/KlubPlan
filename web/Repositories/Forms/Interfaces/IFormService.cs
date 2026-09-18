using web.Repositories.Forms.Dtos;
using web.ViewModels;

namespace web.Repositories.Forms.Interfaces
{
    public interface IFormService
    {
        Task<FormFilterViewModel> GetFormsAsync(FormFilterViewModel filter, string userId, bool isAdmin, CancellationToken ct = default);

        Task<FormBuilderViewModel?> GetFormForBuilderAsync(int id, CancellationToken ct = default);

        Task<SaveFormResponseDto> SaveFormAsync(SaveFormRequestDto dto, CancellationToken ct = default);

        Task<bool> DeleteFormAsync(int id, CancellationToken ct = default);

        Task<SaveFormResponseDto> CreateNewVersionAsync(int sourceFormId, string? userId, CancellationToken ct = default);

        Task<FormFillViewModel?> GetFormForFillAsync(int id, CancellationToken ct = default);

        Task<SubmitFormResponseDto> SubmitFormAsync(SubmitFormRequestDto dto, CancellationToken ct = default);

        /// <summary>Access gate + form data for the public, unauthenticated /Formular link. formPublicId is Form.PublicId, not Form.Id.</summary>
        Task<PublicFormAccessViewModel> GetPublicFormAsync(Guid formPublicId, Guid? personPublicId, CancellationToken ct = default);

        Task<SubmitPublicFormResponseDto> SubmitPublicFormAsync(SubmitPublicFormRequestDto dto, CancellationToken ct = default);

        Task<FormResponsesViewModel?> GetResponsesAsync(int formId, int page, int pageSize, CancellationToken ct = default);

        /// <summary>Rows for CSV export: one row per submission, in the same column order as GetResponsesAsync.</summary>
        Task<FormResponsesViewModel?> GetAllResponsesForExportAsync(int formId, CancellationToken ct = default);
    }
}
