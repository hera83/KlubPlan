using System.Text.Json.Serialization;

namespace web.Constants
{
    /// <summary>
    /// The role a Person has within the club. Stored on Person.Type as a string
    /// (see ApplicationDbContext) so values stay readable in the database and stable across
    /// enum reordering. [JsonConverter] ensures the same string round-trips through the
    /// PersonDetails JSON endpoint used to prefill the edit modal.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PersonType
    {
        Player,
        Coach,
        YouthCoach
    }

    /// <summary>
    /// Danish UI labels and Bootstrap Icons for each person type.
    /// </summary>
    public static class PersonTypes
    {
        private static readonly Dictionary<PersonType, string> _uiLabels = new()
        {
            { PersonType.Player, "Spiller" },
            { PersonType.Coach, "Træner" },
            { PersonType.YouthCoach, "Ungtræner" }
        };

        private static readonly Dictionary<PersonType, string> _icons = new()
        {
            { PersonType.Player, "bi-person-fill" },
            { PersonType.Coach, "bi-person-badge-fill" },
            { PersonType.YouthCoach, "bi-mortarboard-fill" }
        };

        public static string GetUILabel(PersonType type)
            => _uiLabels.TryGetValue(type, out var label) ? label : type.ToString();

        public static string GetIcon(PersonType type)
            => _icons.TryGetValue(type, out var icon) ? icon : "bi-person-fill";

        public static IReadOnlyList<PersonType> AllTypes { get; } = Enum.GetValues<PersonType>();
    }
}
