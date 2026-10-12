namespace web.Data.Models;

/// <summary>
/// The answer given to a single FormField within one FormSubmission.
/// Checkboxes (multi-select) store their selected options joined with ", ".
/// </summary>
public class FormAnswer : EntityBase
{
    public int FormSubmissionId { get; set; }

    public int FormFieldId { get; set; }

    public string? ValueText { get; set; }

    public virtual FormSubmission FormSubmission { get; set; } = null!;

    public virtual FormField FormField { get; set; } = null!;
}
