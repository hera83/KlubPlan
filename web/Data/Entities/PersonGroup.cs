namespace web.Data.Entities
{
    /// <summary>
    /// A named group persons can be assigned to (e.g. a team or a committee). A person can belong
    /// to multiple groups. Deleting a group only removes memberships, never the persons in it.
    /// </summary>
    public class PersonGroup
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ICollection<PersonGroupMembership> Memberships { get; set; } = new List<PersonGroupMembership>();
    }
}
