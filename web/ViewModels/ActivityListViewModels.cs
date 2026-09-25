using System.ComponentModel.DataAnnotations;
using web.Constants;
using web.Data.Entities;

namespace web.ViewModels
{
    /// <summary>A row in the "Lister" tab's table on the activity.</summary>
    public class ActivityListSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SourceFileName { get; set; }
        public int ItemCount { get; set; }

        /// <summary>Lines assigned to the current user (via their workgroup membership).</summary>
        public int MineCount { get; set; }

        public int UnassignedCount { get; set; }

        /// <summary>In status order — drives the stacked progress bar.</summary>
        public List<ActivityListStatusViewModel> Statuses { get; set; } = new();

        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedByName { get; set; }
    }

    public class ActivityListCreateViewModel
    {
        [Required]
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Angiv en titel.")]
        [StringLength(200, ErrorMessage = "Titlen må højst være {1} tegn.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Beskrivelsen må højst være {1} tegn.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vælg en Excel-fil.")]
        public IFormFile? File { get; set; }
    }

    public class ActivityListUpdateViewModel
    {
        [Required]
        public int ListId { get; set; }

        [Required(ErrorMessage = "Angiv en titel.")]
        [StringLength(200, ErrorMessage = "Titlen må højst være {1} tegn.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Beskrivelsen må højst være {1} tegn.")]
        public string? Description { get; set; }
    }

    /// <summary>Columns, statuses and workgroup members of a list — everything needed to render a line.</summary>
    public class ActivityListSchemaViewModel
    {
        public int ListId { get; set; }
        public bool ShowNote { get; set; }
        public List<ActivityListColumnViewModel> Columns { get; set; } = new();
        public List<ActivityListStatusViewModel> Statuses { get; set; } = new();
        public List<ActivityListMemberViewModel> Members { get; set; } = new();

        public IEnumerable<ActivityListColumnViewModel> ImportedColumns => Columns.Where(c => c.IsImported);
        public IEnumerable<ActivityListColumnViewModel> ExtraColumns => Columns.Where(c => !c.IsImported);

        /// <summary>Columns shown in the table; hidden columns are still exported.</summary>
        public List<ActivityListColumnViewModel> VisibleColumns => Columns.Where(c => !c.IsHidden).ToList();
    }

    public class ActivityListColumnViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ActivityListColumnKind Kind { get; set; }
        public List<string> Options { get; set; } = new();
        public bool IsHidden { get; set; }
        public bool IsImported => Kind == ActivityListColumnKind.Imported;

        public string KindLabel => Kind switch
        {
            ActivityListColumnKind.Imported => "Fra Excel",
            ActivityListColumnKind.Text => "Tekst",
            ActivityListColumnKind.YesNo => "Ja/nej",
            ActivityListColumnKind.Choice => "Valgliste",
            _ => Kind.ToString()
        };
    }

    public class ActivityListStatusViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "muted";
        public bool IsDefault { get; set; }
        public int Count { get; set; }
    }

    public class ActivityListMemberViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Role { get; set; }

        /// <summary>True for a workgroup member linked to an administrator account (can use "Kun mine").</summary>
        public bool IsLinkedUser { get; set; }

        /// <summary>True when this member is the current user.</summary>
        public bool IsMe { get; set; }

        public int AssignedCount { get; set; }

        /// <summary>External contacts only — used by "Send links" (a linked administrator logs in instead).</summary>
        public string? Email { get; set; }

        public string? Mobile { get; set; }

        /// <summary>ActivityWorkgroupMember.PublicId — the UId in the member's public /Arbejdsliste link.</summary>
        public Guid PublicId { get; set; }
    }

    /// <summary>The list page (ActivityLists/Details).</summary>
    public class ActivityListDetailsViewModel
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public string ActivityTitle { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SourceFileName { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int TotalCount { get; set; }
        public int UnassignedCount { get; set; }

        /// <summary>False when the current user isn't a (linked) member of the activity's workgroup — "Kun mine" is then disabled.</summary>
        public bool CurrentUserIsMember { get; set; }

        public ActivityListSchemaViewModel Schema { get; set; } = new();
        public ActivityListItemsViewModel Items { get; set; } = new();
    }

    /// <summary>Query of the list page's table (search, filter, sort, paging) — bound from the data-table query string.</summary>
    public class ActivityListItemFilterViewModel
    {
        public int ListId { get; set; }
        public string? SearchText { get; set; }
        public int? StatusId { get; set; }

        /// <summary>"mine", "none" or a workgroup member id.</summary>
        public string? Assigned { get; set; }

        /// <summary>"with" / "without" note.</summary>
        public string? NoteFilter { get; set; }

        /// <summary>"order" (default), "status", "assigned", "note" or "c{columnId}".</summary>
        public string? SortColumn { get; set; }

        /// <summary>"asc" (default) or "desc".</summary>
        public string? SortDir { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public bool HasFilter => !string.IsNullOrWhiteSpace(SearchText) || StatusId.HasValue
            || !string.IsNullOrWhiteSpace(Assigned) || !string.IsNullOrWhiteSpace(NoteFilter);
    }

    public class ActivityListItemsViewModel : ActivityListItemFilterViewModel
    {
        public ActivityListSchemaViewModel Schema { get; set; } = new();
        public List<ActivityListItemRowViewModel> Rows { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class ActivityListItemRowViewModel
    {
        public int Id { get; set; }
        public int RowNumber { get; set; }
        public Dictionary<int, string?> Values { get; set; } = new();
        public int? StatusId { get; set; }
        public string? Note { get; set; }
        public int? AssignedMemberId { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedByName { get; set; }

        /// <summary>Copies to print for the line (sum of its labels' quantities) — shown on the "Labels" button.</summary>
        public int LabelCopies { get; set; }
    }

    /// <summary>Inline edit of one field on one line (autosave).</summary>
    public class ActivityListFieldUpdateViewModel
    {
        [Required]
        public int ListId { get; set; }

        [Required]
        public int ItemId { get; set; }

        /// <summary>"status", "note", "assigned" or "column".</summary>
        [Required]
        public string Field { get; set; } = string.Empty;

        public int? ColumnId { get; set; }

        [StringLength(4000)]
        public string? Value { get; set; }
    }

    /// <summary>"Rediger linje" / "Tilføj linje" — values keyed by column id (Values[12]=...).</summary>
    public class ActivityListItemSaveViewModel
    {
        [Required]
        public int ListId { get; set; }

        /// <summary>Null/0 = new line.</summary>
        public int? ItemId { get; set; }

        public Dictionary<int, string?> Values { get; set; } = new();
    }

    public class ActivityListDistributeViewModel
    {
        [Required]
        public int ListId { get; set; }

        public List<int> MemberIds { get; set; } = new();

        /// <summary>True = only lines without Tilknyttet are distributed; false = all lines are redistributed.</summary>
        public bool OnlyUnassigned { get; set; } = true;
    }

    public class ActivityListColumnSaveViewModel
    {
        [Required]
        public int ListId { get; set; }

        /// <summary>Null = new column.</summary>
        public int? ColumnId { get; set; }

        [Required(ErrorMessage = "Angiv et kolonnenavn.")]
        [StringLength(200, ErrorMessage = "Navnet må højst være {1} tegn.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Only used for a new column: Text, YesNo or Choice.</summary>
        public ActivityListColumnKind Kind { get; set; } = ActivityListColumnKind.Text;

        /// <summary>Choice columns: one option per line.</summary>
        [StringLength(2000)]
        public string? Options { get; set; }
    }

    public class ActivityListStatusesSaveViewModel
    {
        [Required]
        public int ListId { get; set; }

        public List<ActivityListStatusRowInput> Statuses { get; set; } = new();

        /// <summary>Index into Statuses of the default status for new lines.</summary>
        public int DefaultIndex { get; set; }
    }

    public class ActivityListStatusRowInput
    {
        /// <summary>Null/0 = new status.</summary>
        public int? Id { get; set; }

        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = "muted";
    }

    /// <summary>"Send links" — each selected external contact gets their own /Arbejdsliste link by e-mail and/or SMS.</summary>
    public class ActivityListSendLinksViewModel
    {
        [Required]
        public int ListId { get; set; }

        public List<int> MemberIds { get; set; } = new();

        public bool ViaEmail { get; set; }

        public bool ViaSms { get; set; }

        [StringLength(1000, ErrorMessage = "Beskeden må højst være {1} tegn.")]
        public string? Message { get; set; }
    }

    // ── Labels ──────────────────────────────────────────────────────────────

    /// <summary>The line's labels, loaded into the "Labels" modal.</summary>
    public class ActivityListItemLabelsViewModel
    {
        public int ItemId { get; set; }
        public int RowNumber { get; set; }
        public List<ActivityListLabelInput> Labels { get; set; } = new();
    }

    /// <summary>"Labels" modal → Gem. Replaces all labels on the line; rows without text are skipped.</summary>
    public class ActivityListItemLabelsSaveViewModel
    {
        [Required]
        public int ListId { get; set; }

        [Required]
        public int ItemId { get; set; }

        [MaxLength(ActivityListRules.MaxLabelsPerItem, ErrorMessage = "En linje kan højst have {1} labels.")]
        public List<ActivityListLabelInput> Labels { get; set; } = new();
    }

    public class ActivityListLabelInput
    {
        [StringLength(ActivityListRules.MaxLabelTextLength, ErrorMessage = "Teksten på en label må højst være {1} tegn.")]
        public string? Text { get; set; }

        [Range(1, ActivityListRules.MaxLabelQuantity, ErrorMessage = "Antal skal være mellem {1} og {2}.")]
        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// "Print labels": the A4 sheet's grid (labels across × down, portrait or landscape) plus the
    /// list filter — only set when "Kun linjer i nuværende filter" is chosen.
    /// </summary>
    public class ActivityListLabelPrintViewModel : ActivityListItemFilterViewModel
    {
        [Range(1, ActivityListRules.MaxLabelsAcross, ErrorMessage = "Labels i bredden skal være mellem {1} og {2}.")]
        public int Across { get; set; } = 3;

        [Range(1, ActivityListRules.MaxLabelsDown, ErrorMessage = "Labels i højden skal være mellem {1} og {2}.")]
        public int Down { get; set; } = 8;

        public bool Landscape { get; set; }
    }

    // ── Offentligt link (Arbejdsliste) ──────────────────────────────────────

    /// <summary>
    /// The public, unauthenticated /Arbejdsliste?Id={listId}&amp;UId={member.PublicId} page: an external
    /// workgroup contact sees and works on only the lines assigned to them. Status NotFound covers a
    /// wrong/old link, a list from another activity and a member that is (now) a linked administrator.
    /// </summary>
    public class PublicWorkListViewModel
    {
        public bool Found { get; set; }
        public int ListId { get; set; }
        public Guid MemberPublicId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string ActivityTitle { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>Statuses with counts for the member's own lines only.</summary>
        public ActivityListSchemaViewModel Schema { get; set; } = new();
        public PublicWorkListItemsViewModel Items { get; set; } = new();
    }

    public class PublicWorkListItemsViewModel : ActivityListItemsViewModel
    {
        public Guid MemberPublicId { get; set; }
    }
}
