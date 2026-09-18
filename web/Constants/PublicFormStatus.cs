namespace web.Constants
{
    /// <summary>
    /// Result of resolving access to a form via the public, unauthenticated /Formular link.
    /// </summary>
    public enum PublicFormStatus
    {
        /// <summary>Form found, open, and (if not anonymous) tied to a valid person — safe to fill out.</summary>
        Ok,
        NotFound,
        Closed,
        MissingIdentity,
        InvalidIdentity,
        AlreadySubmitted
    }
}
