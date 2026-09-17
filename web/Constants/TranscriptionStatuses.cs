using System.Text.Json.Serialization;

namespace web.Constants
{
    /// <summary>
    /// Status of a background transcription job for a meeting attachment. Stored on
    /// MeetingAttachment.TranscriptionStatus as a string (see ApplicationDbContext).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TranscriptionStatus
    {
        None,
        Queued,
        Processing,
        Completed,
        Failed
    }

    /// <summary>
    /// Danish UI labels and badge classes for each transcription status.
    /// </summary>
    public static class TranscriptionStatuses
    {
        private static readonly Dictionary<TranscriptionStatus, string> _uiLabels = new()
        {
            { TranscriptionStatus.None, string.Empty },
            { TranscriptionStatus.Queued, "Venter..." },
            { TranscriptionStatus.Processing, "Transskriberer..." },
            { TranscriptionStatus.Completed, "Transskriberet" },
            { TranscriptionStatus.Failed, "Transskription mislykkedes" }
        };

        private static readonly Dictionary<TranscriptionStatus, string> _badgeClasses = new()
        {
            { TranscriptionStatus.Queued, "badge-info" },
            { TranscriptionStatus.Processing, "badge-info" },
            { TranscriptionStatus.Completed, "badge-ok" },
            { TranscriptionStatus.Failed, "badge-alarm" }
        };

        public static string GetUILabel(TranscriptionStatus status)
            => _uiLabels.TryGetValue(status, out var label) ? label : status.ToString();

        public static string GetBadgeClass(TranscriptionStatus status)
            => _badgeClasses.TryGetValue(status, out var cls) ? cls : "badge-info";

        public static bool IsInProgress(TranscriptionStatus status)
            => status is TranscriptionStatus.Queued or TranscriptionStatus.Processing;
    }
}
