namespace web.Data.Entities
{
    /// <summary>
    /// Join entity for the many-to-many relationship between Person and PersonGroup.
    /// </summary>
    public class PersonGroupMembership
    {
        public int PersonId { get; set; }

        public int GroupId { get; set; }

        public virtual Person Person { get; set; } = null!;

        public virtual PersonGroup Group { get; set; } = null!;
    }
}
