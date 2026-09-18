using web.Constants;

namespace web.Repositories.Registrations.Dtos
{
    public class SubmitPublicArrangementResponseDto
    {
        /// <summary>Access gate outcome (NotFound/Closed/MissingIdentity/InvalidIdentity/NotAllowed/AlreadyRegistered/Ok).</summary>
        public PublicArrangementStatus Status { get; set; }

        /// <summary>Only meaningful when Status is Ok — whether the submitted answers/shift selection passed validation.</summary>
        public bool Success { get; set; }

        /// <summary>ArrangementFormFieldId -> validation error, used to re-highlight the sign-up form.</summary>
        public Dictionary<int, string> FieldErrors { get; set; } = new();

        /// <summary>Set when shift selection itself failed validation.</summary>
        public string? ShiftError { get; set; }

        /// <summary>Set on Success, for the thank-you page — avoids re-resolving the gate after the registration was written (it would now report AlreadyRegistered).</summary>
        public string? ArrangementTitle { get; set; }
    }
}
