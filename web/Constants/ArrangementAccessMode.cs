using System.Text.Json.Serialization;

namespace web.Constants
{
    /// <summary>
    /// Controls who may register for an Arrangement. Stored on Arrangement.AccessMode as a
    /// string (see ApplicationDbContext) so values stay readable in the database and stable
    /// across enum reordering.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ArrangementAccessMode
    {
        Open,
        Restricted
    }

    /// <summary>
    /// Danish UI labels for each access mode.
    /// </summary>
    public static class ArrangementAccessModes
    {
        private static readonly Dictionary<ArrangementAccessMode, string> _uiLabels = new()
        {
            { ArrangementAccessMode.Open, "Åben for alle" },
            { ArrangementAccessMode.Restricted, "Begrænset adgang" }
        };

        public static string GetUILabel(ArrangementAccessMode mode)
            => _uiLabels.TryGetValue(mode, out var label) ? label : mode.ToString();

        public static IReadOnlyList<ArrangementAccessMode> AllModes { get; } = Enum.GetValues<ArrangementAccessMode>();
    }
}
