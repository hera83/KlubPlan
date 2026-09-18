using System.Text.Json.Serialization;

namespace web.Constants
{
    /// <summary>
    /// Status of a CommunicationMessage. Stored on CommunicationMessage.Status as a string (see
    /// ApplicationDbContext) so values stay readable in the database and stable across enum reordering.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CommunicationMessageStatus
    {
        Draft,
        Sending,
        Sent,
        Failed
    }

    /// <summary>
    /// Danish UI labels and badge classes for each communication message status.
    /// </summary>
    public static class CommunicationMessageStatuses
    {
        private static readonly Dictionary<CommunicationMessageStatus, string> _uiLabels = new()
        {
            { CommunicationMessageStatus.Draft, "Kladde" },
            { CommunicationMessageStatus.Sending, "Sender..." },
            { CommunicationMessageStatus.Sent, "Sendt" },
            { CommunicationMessageStatus.Failed, "Fejlet" }
        };

        private static readonly Dictionary<CommunicationMessageStatus, string> _badgeClasses = new()
        {
            { CommunicationMessageStatus.Draft, "badge-warn" },
            { CommunicationMessageStatus.Sending, "badge-info" },
            { CommunicationMessageStatus.Sent, "badge-ok" },
            { CommunicationMessageStatus.Failed, "badge-alarm" }
        };

        public static string GetUILabel(CommunicationMessageStatus status)
            => _uiLabels.TryGetValue(status, out var label) ? label : status.ToString();

        public static string GetBadgeClass(CommunicationMessageStatus status)
            => _badgeClasses.TryGetValue(status, out var cls) ? cls : "badge-info";
    }
}
