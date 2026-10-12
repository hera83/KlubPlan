using web.Constants;

namespace web.Managers.Arrangements.Dtos;

public class SubmitPublicArrangementResultDto
{
    /// <summary>Access gate outcome (NotFound/NotYetOpen/Closed/MissingIdentity/InvalidIdentity/NotAllowed/AlreadyRegistered/Ok).</summary>
    public PublicArrangementStatus Status { get; set; }

    /// <summary>Only meaningful when Status is Ok — whether answers and shift selection passed validation and were saved.</summary>
    public bool Success { get; set; }

    /// <summary>ArrangementFormFieldId -> validation error, used to re-highlight the sign-up form.</summary>
    public Dictionary<int, string> FieldErrors { get; set; } = [];

    /// <summary>Set when the shift selection itself failed validation.</summary>
    public string? ShiftError { get; set; }

    /// <summary>Set on Success, for the thank-you page (re-resolving the gate would now say AlreadyRegistered).</summary>
    public string? ArrangementTitle { get; set; }
}
