using web.Constants;

namespace web.Managers.Forms.Dtos;

public class SubmitPublicFormResultDto
{
    /// <summary>Access gate outcome (NotFound/Closed/MissingIdentity/InvalidIdentity/AlreadySubmitted/Ok).</summary>
    public PublicFormStatus Status { get; set; }

    /// <summary>Only meaningful when Status is Ok — whether the answers passed field validation and were saved.</summary>
    public bool Success { get; set; }

    /// <summary>FormFieldId -> validation error, used to re-highlight the fill-out form.</summary>
    public Dictionary<int, string> FieldErrors { get; set; } = [];

    /// <summary>Set on Success, for the thank-you page (re-resolving the gate would now say AlreadySubmitted).</summary>
    public string? FormTitle { get; set; }
}
