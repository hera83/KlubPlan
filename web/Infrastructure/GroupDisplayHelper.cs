namespace web.Infrastructure
{
    public static class GroupDisplayHelper
    {
        /// <summary>True when a selection of groups covers every group that exists in the system — the "Alle" case.</summary>
        public static bool IsAllGroups(int selectedCount, int totalGroupCount)
            => totalGroupCount > 0 && selectedCount == totalGroupCount;
    }
}
