namespace web.Data.Entities
{
    /// <summary>
    /// A person tracked by the club (member, participant, etc.), independent of ApplicationUser
    /// (a Person does not need a login). Can belong to multiple PersonGroups and have any number
    /// of PersonGuardian contacts. A person's role (Spiller/Træner/Ungtræner) is per group
    /// membership — see PersonGroupMembership.Type — since the same person can be a player in one
    /// group and a coach in another.
    /// </summary>
    public class Person
    {
        public int Id { get; set; }

        /// <summary>
        /// Club-assigned member number. User-editable; auto-generated (sequential, zero-padded)
        /// when left blank on creation. Always unique.
        /// </summary>
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// Unguessable id used in public, unauthenticated links (e.g. /Formular?Id=..&amp;UId=..)
        /// so a person can be identified without exposing the sequential, human-readable Uid.
        /// </summary>
        public Guid PublicId { get; set; } = Guid.NewGuid();

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
