using web.Constants;

namespace web.Data.Entities
{
    /// <summary>
    /// Join entity for the many-to-many relationship between Person and PersonGroup. Carries the
    /// person's role (Spiller/Træner/Ungtræner) within that specific group, since the same person
    /// can be a player in one group and a coach in another. Defaults to Spiller.
    /// </summary>
    public class PersonGroupMembership
    {
        public int PersonId { get; set; }

        public int GroupId { get; set; }

        public PersonType Type { get; set; } = PersonType.Player;

        public virtual Person Person { get; set; } = null!;

        public virtual PersonGroup Group { get; set; } = null!;
    }
}
