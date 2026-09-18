using web.Constants;

namespace web.Repositories.Forms.Dtos
{
    public class SubmitPublicFormResponseDto
    {
        /// <summary>Access gate outcome (NotFound/Closed/MissingIdentity/InvalidIdentity/AlreadySubmitted/Ok).</summary>
        public PublicFormStatus Status { get; set; }

        /// <summary>Only meaningful when Status is Ok — whether the submitted answers passed field validation.</summary>
        public bool Success { get; set; }

        /// <summary>FormFieldId -> validation error, used to re-highlight the fill-out form.</summary>
        public Dictionary<int, string> FieldErrors { get; set; } = new();

        /// <summary>Set on Success, for the thank-you page — avoids re-resolving the gate after the submission was written (it would now report AlreadySubmitted).</summary>
        public string? FormTitle { get; set; }
    }
}
