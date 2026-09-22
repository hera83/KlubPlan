namespace web.ViewModels
{
    /// <summary>
    /// The recipient/attachment picker options for the shared "Ny besked" compose modal
    /// (_ComposeMessageModal.cshtml) — used both by CommunicationIndexViewModel (via Communication/
    /// Index) and standalone when composing a message from an Activity's detail page.
    /// </summary>
    public class ComposeOptionsViewModel
    {
        public List<PersonGroupOptionViewModel> GroupOptions { get; set; } = new();
        public List<ArrangementPersonOptionViewModel> PersonOptions { get; set; } = new();
        public List<CommunicationFormOptionViewModel> FormOptions { get; set; } = new();
        public List<CommunicationArrangementOptionViewModel> ArrangementOptions { get; set; } = new();
    }

    public class CommunicationIndexViewModel : ComposeOptionsViewModel
    {
        public bool IsAdmin { get; set; }
        public List<CommunicationMessageListItemViewModel> Messages { get; set; } = new();
    }

    public class CommunicationMessageListItemViewModel
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public bool ViaEmail { get; set; }
        public bool ViaSms { get; set; }
        public CommunicationRecipientBadgesViewModel RecipientBadges { get; set; } = new();
        public int RecipientCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = "badge-info";
        public bool IsDraft { get; set; }
        public DateTime? SentAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>Selected groups + directly-selected persons, for the compact badge display in the table and on Details (mirrors Meetings' group-badge treatment).</summary>
    public class CommunicationRecipientBadgesViewModel
    {
        public List<PersonGroupOptionViewModel> Groups { get; set; } = new();

        /// <summary>True when every group in the system is selected — shows a single "Alle"-badge instead of looping Groups.</summary>
        public bool IsAllGroups { get; set; }

        public List<string> DirectPersonNames { get; set; } = new();
    }

    public class CommunicationFormOptionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsAnonymous { get; set; }
    }

    /// <summary>Unlike CommunicationFormOptionViewModel, there is no IsAnonymous flag — Tilmelding links are always personal.</summary>
    public class CommunicationArrangementOptionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        /// <summary>True when the arrangement's own Adgang-tab restricts sign-up to specific persons/groups.</summary>
        public bool IsRestricted { get; set; }

        /// <summary>Mirrors the arrangement's AllowedPersons — used to auto-select and lock the compose modal's recipient pickers so a personal Tilmelding link can't be sent to someone without access.</summary>
        public List<int> AllowedPersonIds { get; set; } = new();

        /// <summary>Mirrors the arrangement's AllowedGroups — same purpose as AllowedPersonIds.</summary>
        public List<int> AllowedGroupIds { get; set; } = new();

        /// <summary>Danish sentence stating when registration opens (or that it's already open), inserted into the message body when this arrangement is selected so recipients know upfront when they can pick shifts.</summary>
        public string DefaultLinkText { get; set; } = string.Empty;
    }

    public class CommunicationMessageDetailViewModel
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = "badge-info";
        public string RecipientSummary { get; set; } = string.Empty;
        public CommunicationRecipientBadgesViewModel RecipientBadges { get; set; } = new();
        public bool IsSent { get; set; }
        public bool ViaEmail { get; set; }
        public bool ViaSms { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? SentAtUtc { get; set; }

        /// <summary>Name of the attached form, if a form link was sent with the message. Informational only.</summary>
        public string? FormTitle { get; set; }

        /// <summary>Name of the attached arrangement, if a Tilmelding link was sent with the message. Informational only.</summary>
        public string? ArrangementTitle { get; set; }

        /// <summary>Activity this message was sent from, if any — drives the back-link target on Communication/Details.</summary>
        public int? ActivityId { get; set; }

        /// <summary>Page 1 of the full target audience, plus the filter state — feeds the paged/searchable "Modtagere" table (data-table pattern).</summary>
        public CommunicationRecipientFilterViewModel RecipientsTable { get; set; } = new();
    }

    /// <summary>Filter/paging state for the "Modtagere" table on Communication/Details — mirrors the UserFilterViewModel data-table pattern (holds both the request filter and the resulting page).</summary>
    public class CommunicationRecipientFilterViewModel
    {
        public string? SearchText { get; set; }

        /// <summary>"MissingEmail" | "MissingSms" | "Sent" | null (Alle).</summary>
        public string? ContactStatus { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        /// <summary>The full target audience (every person covered by the selected groups/persons), not just those who ended up with a resolvable address.</summary>
        public List<CommunicationRecipientDetailViewModel> Recipients { get; set; } = new();
        public int TotalCount { get; set; }
        public bool ViaEmail { get; set; }
        public bool ViaSms { get; set; }
        public bool IsSent { get; set; }
    }

    public class CommunicationRecipientDetailViewModel
    {
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Addresses this person actually got a message queued to (their own or a guardian's) — empty if none, whether from missing contact info or dedup with another recipient.</summary>
        public List<CommunicationRecipientAddressViewModel> EmailAddresses { get; set; } = new();
        public List<CommunicationRecipientAddressViewModel> SmsAddresses { get; set; } = new();

        /// <summary>True if the person or any of their guardians has an email/mobile at all, independent of whether it was deduped away by another recipient.</summary>
        public bool HasEmailContact { get; set; }
        public bool HasSmsContact { get; set; }
    }

    public class CommunicationRecipientAddressViewModel
    {
        public string Address { get; set; } = string.Empty;
        public string DeliveryStatus { get; set; } = string.Empty;
    }

    /// <summary>One selectable row in the "Send igen" recipient picker — the full target audience (every person covered by the message's selected groups/persons), independent of whether they ended up with a resolvable address.</summary>
    public class CommunicationResendTargetViewModel
    {
        public int PersonId { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>True if this person has at least one queued send attempt on record for this message (from any previous send/resend) — false means they've never actually been reached, e.g. because they had no contact info at send time. Drives the "Ny" quick-select in the picker.</summary>
        public bool ReceivedBefore { get; set; }
    }
}
