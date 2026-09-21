using System.ComponentModel.DataAnnotations;

namespace web.ViewModels
{
    // ── Liste (Index) ──────────────────────────────────────────────────────

    public class ActivityListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Location { get; set; }
        public DateTime? StartAtUtc { get; set; }
        public DateTime? EndAtUtc { get; set; }
        public bool IsCancelled { get; set; }
        public List<string> TargetGroupNames { get; set; } = new();

        /// <summary>True when every group in the system is selected — the view shows a single "Alle"-badge instead of looping TargetGroupNames.</summary>
        public bool IsAllGroups { get; set; }

        public int TaskTotalCount { get; set; }
        public int TaskCompletedCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class ActivityFilterViewModel
    {
        public string? SearchText { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<ActivityListItemViewModel> Aktiviteter { get; set; } = new();
        public int TotalCount { get; set; }
    }

    // ── Opret/Rediger ────────────────────────────────────────────────────

    public class ActivityFormViewModel
    {
        /// <summary>0 (eller null på GET-routen) betyder en ny aktivitet.</summary>
        public int Id { get; set; }

        [Required(ErrorMessage = "Aktiviteten skal have en titel")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public DateTime? StartAt { get; set; }

        public DateTime? EndAt { get; set; }

        public bool IsCancelled { get; set; }

        public List<int> GroupIds { get; set; } = new();

        /// <summary>Server-populerede options til målgruppe-vælgeren — postes ikke tilbage fra klienten.</summary>
        public List<PersonGroupOptionViewModel> GroupOptions { get; set; } = new();
    }

    // ── Arbejdsgruppe ────────────────────────────────────────────────────

    public class ActivityWorkgroupMemberViewModel
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }

        /// <summary>Set when this member is a linked administrator; null for an external contact.</summary>
        public string? ApplicationUserId { get; set; }

        /// <summary>Effective display name — the linked administrator's DisplayName, or the external contact's own name.</summary>
        [Required(ErrorMessage = "Navn skal udfyldes")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Role { get; set; }

        [StringLength(200)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? Mobile { get; set; }
    }

    // ── Opgaver ──────────────────────────────────────────────────────────

    public class ActivityTaskViewModel
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Opgaven skal have en titel")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public DateTime? DeadlineAtUtc { get; set; }

        public int? AssignedToWorkgroupMemberId { get; set; }

        public string? AssignedToName { get; set; }

        public bool IsCompleted { get; set; }

        [StringLength(1000)]
        public string? Note { get; set; }

        public DateTime? CompletedAtUtc { get; set; }
    }

    // ── Link til Formular ───────────────────────────────────────────────

    public class ActivityFormOptionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    // ── Detaljer ─────────────────────────────────────────────────────────

    public class ActivityDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? Category { get; set; }
        public DateTime? StartAtUtc { get; set; }
        public DateTime? EndAtUtc { get; set; }
        public bool IsCancelled { get; set; }

        public List<PersonGroupOptionViewModel> TargetGroups { get; set; } = new();

        /// <summary>True when every group in the system is selected — shown as a single "Alle"-badge instead of the full list.</summary>
        public bool IsAllGroups { get; set; }

        /// <summary>Distinct persons reachable via the target groups — for the Oversigt mini-dashboard's "Målgruppe"-tile.</summary>
        public int TargetAudienceCount { get; set; }

        public List<ActivityWorkgroupMemberViewModel> WorkgroupMembers { get; set; } = new();

        /// <summary>Active Administrator/Developer users, for the arbejdsgruppe "Vælg administrator" dropdown.</summary>
        public List<MeetingAdminOptionViewModel> AdminOptions { get; set; } = new();

        public List<ActivityTaskViewModel> Tasks { get; set; } = new();

        public int? FormId { get; set; }
        public string? FormTitle { get; set; }

        public List<ActivityFormOptionViewModel> FormOptions { get; set; } = new();

        /// <summary>Populated when FormId is set — first page of responses for the "Svar" tab, same shape as Forms → Vis svar.</summary>
        public FormResponsesViewModel? FormResponses { get; set; }

        public List<CommunicationMessageListItemViewModel> Messages { get; set; } = new();

        /// <summary>Options til den delte "Send besked"-modal (genbruger Kommunikations-modulets compose-UI).</summary>
        public ComposeOptionsViewModel ComposeOptions { get; set; } = new();
    }
}
