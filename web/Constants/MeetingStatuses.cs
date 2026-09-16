using System.Text.Json.Serialization;

namespace web.Constants
{
    /// <summary>
    /// Status of a Meeting. Stored on Meeting.Status as a string (see ApplicationDbContext) so
    /// values stay readable in the database and stable across enum reordering.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MeetingStatus
    {
        Planned,
        Held,
        Cancelled
    }

    /// <summary>
    /// Danish UI labels and Bootstrap Icons for each meeting status.
    /// </summary>
    public static class MeetingStatuses
    {
        private static readonly Dictionary<MeetingStatus, string> _uiLabels = new()
        {
            { MeetingStatus.Planned, "Planlagt" },
            { MeetingStatus.Held, "Afholdt" },
            { MeetingStatus.Cancelled, "Aflyst" }
        };

        private static readonly Dictionary<MeetingStatus, string> _badgeClasses = new()
        {
            { MeetingStatus.Planned, "badge-info" },
            { MeetingStatus.Held, "badge-ok" },
            { MeetingStatus.Cancelled, "badge-alarm" }
        };

        public static string GetUILabel(MeetingStatus status)
            => _uiLabels.TryGetValue(status, out var label) ? label : status.ToString();

        public static string GetBadgeClass(MeetingStatus status)
            => _badgeClasses.TryGetValue(status, out var cls) ? cls : "badge-info";

        public static IReadOnlyList<MeetingStatus> AllStatuses { get; } = Enum.GetValues<MeetingStatus>();
    }
}
