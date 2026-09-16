namespace web.Data.Entities
{
    /// <summary>
    /// A parent/guardian contact for a Person. A person can have any number of these (e.g. both
    /// parents, or a guardian plus a relevant contact for an adult with special needs).
    /// </summary>
    public class PersonGuardian
    {
        public int Id { get; set; }

        public int PersonId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Mobile { get; set; }

        public string? Email { get; set; }

        /// <summary>Display order within the person's guardian list (0-based).</summary>
        public int Order { get; set; }

        public virtual Person Person { get; set; } = null!;
    }
}
