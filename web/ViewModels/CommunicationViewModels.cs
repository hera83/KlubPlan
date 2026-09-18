namespace web.ViewModels
{
    public class CommunicationIndexViewModel
    {
        public bool IsAdmin { get; set; }
        public List<CommunicationMessageListItemViewModel> Messages { get; set; } = new();
        public List<PersonGroupOptionViewModel> GroupOptions { get; set; } = new();
        public List<ArrangementPersonOptionViewModel> PersonOptions { get; set; } = new();
        public List<CommunicationFormOptionViewModel> FormOptions { get; set; } = new();
        public List<CommunicationArrangementOptionViewModel> ArrangementOptions { get; set; } = new();
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

        /// <summary>The full target audience (every person covered by the selected groups/persons), not just those who ended up with a resolvable address.</summary>
        public List<CommunicationRecipientDetailViewModel> Recipients { get; set; } = new();
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
}
