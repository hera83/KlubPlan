using web.Constants;

namespace web.Data.Entities
{
    /// <summary>
    /// An event/activity people can sign up for via shifts ("vagter"). Owns its own signup-form
    /// fields, shifts (with their own per-shift requirement statements) and access list
    /// (Person/PersonGroup). Registrants are identified by Person, not ApplicationUser, since
    /// the eventual self-service sign-up does not require a login.
    /// </summary>
    public class Arrangement
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? RegistrationOpensAtUtc { get; set; }

        public DateTime? RegistrationClosesAtUtc { get; set; }

        /// <summary>
        /// Temporarily forces registration open regardless of RegistrationOpensAtUtc/
        /// RegistrationClosesAtUtc, toggled from the list. False (default) means "follow the
        /// dates". There is no equivalent forced-closed state — closing is done by editing the
        /// dates directly.
        /// </summary>
        public bool RegistrationForcedOpen { get; set; }

        public ArrangementAccessMode AccessMode { get; set; } = ArrangementAccessMode.Open;

        /// <summary>User who created the arrangement. Nullable so it survives the user being deleted.</summary>
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ICollection<ArrangementFormField> FormFields { get; set; } = new List<ArrangementFormField>();

        public virtual ICollection<ArrangementShift> Shifts { get; set; } = new List<ArrangementShift>();

        public virtual ICollection<ArrangementAllowedPerson> AllowedPersons { get; set; } = new List<ArrangementAllowedPerson>();

        public virtual ICollection<ArrangementAllowedGroup> AllowedGroups { get; set; } = new List<ArrangementAllowedGroup>();
    }
}
