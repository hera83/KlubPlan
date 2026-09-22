using web.Repositories.Communication.Dtos;
using web.ViewModels;

namespace web.Repositories.Communication.Interfaces
{
    public interface ICommunicationService
    {
        Task<CommunicationIndexViewModel> GetIndexDataAsync(bool isAdmin, CancellationToken ct = default);

        /// <summary>The recipient/attachment picker options for the shared compose modal, without the message list — used to feed the modal from Activities/Details.</summary>
        Task<ComposeOptionsViewModel> GetComposeOptionsAsync(CancellationToken ct = default);

        /// <summary>Messages sent/drafted from a given Activity (CommunicationMessage.ActivityId), newest first — for the Activity detail page's Kommunikation tab.</summary>
        Task<List<CommunicationMessageListItemViewModel>> GetMessagesForActivityAsync(int activityId, CancellationToken ct = default);

        Task<CommunicationMessageDetailViewModel?> GetDetailsAsync(int id, CancellationToken ct = default);

        /// <summary>Filtered/paged "Modtagere" table for a message — the data-table endpoint backing Communication/Details' recipients table.</summary>
        Task<CommunicationRecipientFilterViewModel?> GetRecipientsAsync(int id, CommunicationRecipientFilterViewModel filter, CancellationToken ct = default);

        /// <summary>The full target audience (Id + name) for a message's selected groups/persons, used to populate the "Send igen" recipient picker.</summary>
        Task<List<CommunicationResendTargetViewModel>> GetResendTargetsAsync(int id, CancellationToken ct = default);

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

        /// <summary>
        /// (Re-)resolves recipients and sends an existing message — used for sending a saved draft
        /// and for "Send igen". When <paramref name="recipientPersonIds"/> is null/empty, sends to
        /// the message's full configured audience (groups + directly-selected persons), same as
        /// before. When it has entries, only those persons are messaged — filtered server-side down
        /// to the message's own target audience, so the picker can't be used to reach anyone outside
        /// the originally configured groups/persons.
        /// </summary>
        Task<SaveMessageResponseDto> SendExistingAsync(int id, string baseUrl, IReadOnlyCollection<int>? recipientPersonIds = null, CancellationToken ct = default);

        /// <summary>Deletes a message and its group/person selections and recipient audit trail. Queued SmsMessage/CommunicationEmailMessage rows are kept (they're the delivery record, not owned by the message).</summary>
        Task<bool> DeleteMessageAsync(int id, CancellationToken ct = default);

        /// <summary>Reads a message attachment's physical file for download — null if the attachment or its file no longer exists.</summary>
        Task<(byte[] Data, string ContentType, string FileName)?> GetAttachmentFileAsync(int attachmentId, CancellationToken ct = default);
    }
}
