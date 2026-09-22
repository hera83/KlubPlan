namespace web.ViewModels
{
    /// <summary>
    /// Generic model for the shared Views/Shared/Partials/_GroupBadges.cshtml partial — renders a
    /// group-selection (target groups, memberships, recipients, ...) as the standard hover-expand
    /// pill list, capped at MaxVisible with a "+N" overflow badge, or a single "Alle"-badge when
    /// IsAll is set. Use this everywhere a list of PersonGroups is shown so the truncation/"Alle"
    /// behavior stays identical across the app.
    /// </summary>
    public class GroupBadgesViewModel
    {
        /// <summary>True when the selection covers every group in the system — renders a single "Alle"-badge instead of the list.</summary>
        public bool IsAll { get; set; }

        public List<GroupBadgeItemViewModel> Items { get; set; } = new();

        /// <summary>True for a comma-separated header/title line (groups-inline); false for a wrapped table-cell pill list.</summary>
        public bool Inline { get; set; }

        public int MaxVisible { get; set; } = 4;
    }

    public class GroupBadgeItemViewModel
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional Bootstrap Icon class shown before the name, e.g. PersonTypes.GetIcon(...).</summary>
        public string? IconClass { get; set; }

        /// <summary>Optional native title-attribute text, e.g. a role label.</summary>
        public string? Title { get; set; }
    }
}
