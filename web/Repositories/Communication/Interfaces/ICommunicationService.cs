using web.Repositories.Communication.Dtos;
using web.ViewModels;

namespace web.Repositories.Communication.Interfaces
{
    public interface ICommunicationService
    {
        Task<CommunicationIndexViewModel> GetIndexDataAsync(bool isAdmin, CancellationToken ct = default);

        Task<CommunicationMessageDetailViewModel?> GetDetailsAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Resolves the deduped set of (Channel, Address) send targets for a group/person selection:
        /// each target Person's own contact info plus every one of their guardians' contact info,
        /// for whichever channels are enabled. See CommunicationService.ResolveRecipientsAsync for
        /// the dedup rule.
        /// </summary>
        Task<List<ResolvedRecipientDto>> ResolveRecipientsAsync(
            IReadOnlyCollection<int> groupIds,
            IReadOnlyCollection<int> personIds,
            bool viaEmail,
            bool viaSms,
            CancellationToken ct = default);

        /// <summary>Creates or updates a Draft message, optionally sending it immediately (dto.Action == "send").</summary>
        Task<SaveMessageResponseDto> SaveMessageAsync(ComposeMessageRequestDto dto, string? userId, string baseUrl, CancellationToken ct = default);

        /// <summary>(Re-)resolves recipients and sends an existing message — used for sending a saved draft and for "Send igen".</summary>
        Task<SaveMessageResponseDto> SendExistingAsync(int id, string baseUrl, CancellationToken ct = default);
    }
}
