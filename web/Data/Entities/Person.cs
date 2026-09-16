namespace web.Data.Entities
{
    /// <summary>
    /// A person tracked by the club (member, participant, etc.), independent of ApplicationUser
    /// (a Person does not need a login). Can belong to multiple PersonGroups and have any number
    /// of PersonGuardian contacts.
    /// </summary>
    public class Person
    {
        public int Id { get; set; }

        /// <summary>
        /// Club-assigned member number. User-editable; auto-generated (sequential, zero-padded)
        /// when left blank on creation. Always unique.
        /// </summary>
        public string Uid { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public DateOnly? BirthDate { get; set; }

        public string? Mobile { get; set; }

        public string? Email { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ICollection<PersonGroupMembership> Memberships { get; set; } = new List<PersonGroupMembership>();

        public virtual ICollection<PersonGuardian> Guardians { get; set; } = new List<PersonGuardian>();
    }
}
