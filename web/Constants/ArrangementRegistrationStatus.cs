namespace web.Constants
{
    /// <summary>
    /// Whether an arrangement's Tilmelding is open, not yet open, or closed. Computed on demand
    /// from RegistrationOpensAtUtc/RegistrationClosesAtUtc/RegistrationForcedOpen — never persisted.
    /// </summary>
    public enum ArrangementRegistrationStatus
    {
        NotYetOpen,
        Open,
        Closed
    }

    /// <summary>
    /// Single source of truth for deriving ArrangementRegistrationStatus, so callers (e.g. the
    /// Communication Tilmelding-link picker) don't have to re-derive the RegistrationForcedOpen/
    /// dates logic already shown to admins in the Tilmelding list (_ArrangementsTableBody.cshtml).
    /// </summary>
    public static class ArrangementRegistrationStatuses
    {
        public static ArrangementRegistrationStatus GetStatus(DateTime? opensAtUtc, DateTime? closesAtUtc, bool forcedOpen)
        {
            if (forcedOpen) return ArrangementRegistrationStatus.Open;

            var now = DateTime.UtcNow;
            if (opensAtUtc.HasValue && now < opensAtUtc.Value) return ArrangementRegistrationStatus.NotYetOpen;
            if (closesAtUtc.HasValue && now > closesAtUtc.Value) return ArrangementRegistrationStatus.Closed;
            return ArrangementRegistrationStatus.Open;
        }
    }
}
