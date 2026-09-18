namespace web.Constants
{
    /// <summary>
    /// Result of resolving access to an arrangement via the public, unauthenticated /Tilmelding
    /// link. Mirrors PublicFormStatus, with the extra NotAllowed outcome for Arrangement's access
    /// list (AccessMode.Restricted) — Formularer has no equivalent whitelist. Unlike Formularer,
    /// there is no anonymous mode, so a missing/invalid UId always blocks access.
    /// </summary>
    public enum PublicArrangementStatus
    {
        /// <summary>Arrangement found, registration open, identity resolved and allowed — safe to sign up.</summary>
        Ok,
        NotFound,

        /// <summary>Registration hasn't started yet (now &lt; RegistrationOpensAtUtc) — the view shows a countdown to the opening date instead of the plain "closed" message.</summary>
        NotYetOpen,
        Closed,
        MissingIdentity,
        InvalidIdentity,
        NotAllowed,
        AlreadyRegistered
    }
}
