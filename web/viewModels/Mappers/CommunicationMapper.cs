using web.Constants;
using web.Extensions;
using web.Managers.Communication.Dtos;
using web.Repositories.CommunicationMessages.Dtos;

namespace web.ViewModels.Mappers;

public static class CommunicationMapper
{
    public static ComposeOptionsViewModel ToComposeOptionsViewModel(this ComposeOptionsDto options) =>
        FillComposeOptions(new ComposeOptionsViewModel(), options);

    public static CommunicationIndexViewModel ToCommunicationIndexViewModel(this ComposeOptionsDto options, CommunicationMessageListDto messages, bool isAdmin)
    {
        var model = FillComposeOptions(new CommunicationIndexViewModel(), options);
        model.IsAdmin = isAdmin;
        model.Messages = messages.ToCommunicationMessageListItemViewModels();
        return model;
    }

    public static List<CommunicationMessageListItemViewModel> ToCommunicationMessageListItemViewModels(this CommunicationMessageListDto list) =>
        list.Messages.Select(m => new CommunicationMessageListItemViewModel
        {
            Id = m.Id,
            Subject = m.Subject,
            ViaEmail = m.ViaEmail,
            ViaSms = m.ViaSms,
            RecipientBadges = ToRecipientBadges(m.Groups, m.DirectPersonNames, list.TotalGroupCount),
            RecipientCount = m.RecipientCount,
            Status = CommunicationMessageStatuses.GetUILabel(m.Status),
            StatusBadgeClass = CommunicationMessageStatuses.GetBadgeClass(m.Status),
            IsDraft = m.Status == CommunicationMessageStatus.Draft,
            SentAtUtc = m.SentAtUtc,
            CreatedAtUtc = m.CreatedAt
        }).ToList();

    public static CommunicationMessageDetailViewModel ToCommunicationMessageDetailViewModel(this CommunicationMessageDetailsDto details)
    {
        var message = details.Message;
        return new CommunicationMessageDetailViewModel
        {
            Id = message.Id,
            Subject = message.Subject,
            Body = message.Body,
            Status = CommunicationMessageStatuses.GetUILabel(message.Status),
            StatusBadgeClass = CommunicationMessageStatuses.GetBadgeClass(message.Status),
            RecipientSummary = message.RecipientSummary,
            RecipientBadges = ToRecipientBadges(message.Groups, message.DirectPersonNames, details.TotalGroupCount),
            IsSent = message.SentAtUtc.HasValue,
            ViaEmail = message.ViaEmail,
            ViaSms = message.ViaSms,
            CreatedAtUtc = message.CreatedAt,
            SentAtUtc = message.SentAtUtc,
            FormTitle = message.FormTitle,
            ArrangementTitle = message.ArrangementTitle,
            ActivityId = message.ActivityId,
            RecipientsTable = details.Recipients.ToCommunicationRecipientFilterViewModel(new SearchCommunicationRecipientsDto()),
            Attachments = message.Attachments
                .OrderBy(a => a.OriginalFileName)
                .Select(a => new CommunicationMessageAttachmentViewModel
                {
                    Id = a.Id,
                    OriginalFileName = a.OriginalFileName,
                    FileSizeBytes = a.FileSizeBytes
                })
                .ToList()
        };
    }

    public static SearchCommunicationRecipientsDto ToSearchCommunicationRecipientsDto(this CommunicationRecipientFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        ContactStatus = filter.ContactStatus,
        Page = filter.Page,
        PageSize = filter.PageSize
    };

    public static CommunicationRecipientFilterViewModel ToCommunicationRecipientFilterViewModel(this CommunicationRecipientPageDto page, SearchCommunicationRecipientsDto search) => new()
    {
        SearchText = search.SearchText,
        ContactStatus = search.ContactStatus,
        Page = page.Page,
        PageSize = page.PageSize,
        TotalCount = page.TotalCount,
        ViaEmail = page.ViaEmail,
        ViaSms = page.ViaSms,
        IsSent = page.IsSent,
        Recipients = page.Items.Select(r => new CommunicationRecipientDetailViewModel
        {
            DisplayName = r.DisplayName,
            EmailAddresses = r.EmailAddresses.Select(a => new CommunicationRecipientAddressViewModel { Address = a.Address, DeliveryStatus = a.DeliveryStatus }).ToList(),
            SmsAddresses = r.SmsAddresses.Select(a => new CommunicationRecipientAddressViewModel { Address = a.Address, DeliveryStatus = a.DeliveryStatus }).ToList(),
            HasEmailContact = r.HasEmailContact,
            HasSmsContact = r.HasSmsContact
        }).ToList()
    };

    public static List<CommunicationResendTargetViewModel> ToCommunicationResendTargetViewModels(this List<CommunicationResendTargetDto> targets) =>
        targets.Select(t => new CommunicationResendTargetViewModel { PersonId = t.PersonId, Name = t.Name, ReceivedBefore = t.ReceivedBefore }).ToList();

    /// <summary>Empty attachment inputs are skipped. The streams belong to the request and close with it.</summary>
    public static SaveCommunicationDto ToSaveCommunicationDto(this ComposeMessageViewModel model, string? userId) => new()
    {
        Id = model.Id,
        Subject = model.Subject,
        Body = model.Body,
        ViaEmail = model.ViaEmail,
        ViaSms = model.ViaSms,
        GroupIds = model.GroupIds,
        PersonIds = model.AllowedPersonIds,
        FormId = model.FormId,
        LinkType = model.LinkType,
        ArrangementId = model.ArrangementId,
        ActivityId = model.ActivityId,
        Send = model.Action == "send",
        UserId = userId,
        Attachments = (model.Attachments ?? [])
            .Where(f => f.Length > 0)
            .Select(f => new CommunicationAttachmentUploadDto { FileName = f.FileName, ContentType = f.ContentType, Content = f.OpenReadStream() })
            .ToList()
    };

    private static T FillComposeOptions<T>(T model, ComposeOptionsDto options) where T : ComposeOptionsViewModel
    {
        model.GroupOptions = options.Groups.Select(g => new PersonGroupOptionViewModel { Id = g.Id, Name = g.Name }).ToList();
        model.PersonOptions = options.People.Select(p => new ArrangementPersonOptionViewModel { Id = p.Id, Uid = p.Uid, Name = p.Name }).ToList();
        model.FormOptions = options.Forms.Select(f => new CommunicationFormOptionViewModel { Id = f.Id, Title = f.Title, IsAnonymous = f.IsAnonymous }).ToList();
        model.ArrangementOptions = options.Arrangements.Select(a => new CommunicationArrangementOptionViewModel
        {
            Id = a.Arrangement.Id,
            Title = a.Arrangement.Title,
            IsRestricted = a.Arrangement.IsRestricted,
            AllowedPersonIds = a.Arrangement.AllowedPersonIds,
            AllowedGroupIds = a.Arrangement.AllowedGroupIds,
            DefaultLinkText = BuildArrangementDefaultLinkText(a.Status, a.Arrangement.RegistrationOpensAtUtc)
        }).ToList();
        return model;
    }

    private static CommunicationRecipientBadgesViewModel ToRecipientBadges(List<CommunicationMessageGroupDto> groups, List<string> directPersonNames, int totalGroupCount)
    {
        var groupOptions = groups
            .Select(g => new PersonGroupOptionViewModel { Id = g.Id, Name = g.Name })
            .OrderBy(g => g.Name)
            .ToList();

        return new CommunicationRecipientBadgesViewModel
        {
            Groups = groupOptions,
            IsAllGroups = GroupDisplayHelper.IsAllGroups(groupOptions.Count, totalGroupCount),
            DirectPersonNames = directPersonNames.OrderBy(n => n).ToList()
        };
    }

    /// <summary>
    /// Danish sentence telling the recipient upfront when they'll be able to pick shifts,
    /// so it can be dropped into the message body as soon as a Tilmelding link is picked.
    /// </summary>
    private static string BuildArrangementDefaultLinkText(ArrangementRegistrationStatus status, DateTime? opensAtUtc)
    {
        if (status == ArrangementRegistrationStatus.Open)
            return "Tilmeldingen er åben nu – I kan allerede gå ind og vælge vagter via linket.";

        return $"Tilmeldingen åbner {opensAtUtc!.Value.FromUtcToLocal().ToDanishDateTimeWithKl()} – I kan gå ind og vælge vagter fra det tidspunkt.";
    }
}
