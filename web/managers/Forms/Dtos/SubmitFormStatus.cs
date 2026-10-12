namespace web.Managers.Forms.Dtos;

public enum SubmitFormStatus
{
    Submitted,
    NotFound,

    /// <summary>The form is closed for responses or isn't the latest version.</summary>
    Closed,

    /// <summary>Required fields are missing (see FieldErrors).</summary>
    Invalid
}
