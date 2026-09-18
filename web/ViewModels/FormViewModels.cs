using System.ComponentModel.DataAnnotations;
using web.Constants;

namespace web.ViewModels
{
    // ── Liste (Index) ──────────────────────────────────────────────────────

    public class FormListItemViewModel
    {
        public int Id { get; set; }

        /// <summary>Used for the public /Formular link instead of Id — see Form.PublicId.</summary>
        public Guid PublicId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsAcceptingResponses { get; set; }
        public bool IsAnonymous { get; set; }
        public int FieldCount { get; set; }
        public int ResponseCount { get; set; }
        public bool HasCurrentUserSubmitted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public int VersionNumber { get; set; }
        public int VersionCount { get; set; }

        /// <summary>All versions in this form's series, for the delete dropdown. Only populated when VersionCount > 1.</summary>
        public List<FormVersionOptionViewModel> Versions { get; set; } = new();
    }

    public class FormVersionOptionViewModel
    {
        public int Id { get; set; }
        public int VersionNumber { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public bool IsAcceptingResponses { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class FormFilterViewModel
    {
        public string? SearchText { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<FormListItemViewModel> Forms { get; set; } = new();
        public int TotalCount { get; set; }
        public bool IsAdmin { get; set; }
    }

    // ── Builder (Create/Edit/Save) ─────────────────────────────────────────

    public class FormFieldBuilderViewModel
    {
        /// <summary>0 for a field that does not exist in the database yet.</summary>
        public int Id { get; set; }

        // Kun påkrævet for besvarelige felter (ikke Informationstekst/SectionHeading) — det
        // tjekkes betinget af FieldType i FormService.SaveFormAsync, ikke her.
        [StringLength(200)]
        public string Label { get; set; } = string.Empty;

        [StringLength(500)]
        public string? HelpText { get; set; }

        public FormFieldType FieldType { get; set; }

        public bool IsRequired { get; set; }

        public int Order { get; set; }

        /// <summary>Raw JSON array of option strings, only meaningful for choice field types.</summary>
        public string? OptionsJson { get; set; }
    }

    public class FormBuilderViewModel
    {
        /// <summary>0 (or null on the GET route) means a new form.</summary>
        public int Id { get; set; }

        [Required(ErrorMessage = "Formularen skal have en titel")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        public bool IsAcceptingResponses { get; set; } = true;

        public bool IsAnonymous { get; set; }

        public List<FormFieldBuilderViewModel> Fields { get; set; } = new();

        public int VersionNumber { get; set; }
        public int VersionCount { get; set; }
        public bool IsLatestVersion { get; set; } = true;

        /// <summary>Id of the latest version in this form's series, set only when IsLatestVersion is false.</summary>
        public int? CurrentVersionId { get; set; }

        /// <summary>Id of the next older version (one VersionNumber lower), for the version-nav arrow. Null when viewing version 1.</summary>
        public int? PreviousVersionId { get; set; }

        /// <summary>Id of the next newer version (one VersionNumber higher), for the version-nav arrow. Null when viewing the latest version.</summary>
        public int? NextVersionId { get; set; }
    }

    // ── Udfyldelse (Fill/Submit) ───────────────────────────────────────────

    public class FormFieldFillViewModel
    {
        public int FormFieldId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? HelpText { get; set; }
        public FormFieldType FieldType { get; set; }
        public bool IsRequired { get; set; }
        public List<string> Options { get; set; } = new();

        /// <summary>Re-populated from a failed POST so the respondent doesn't lose their input.</summary>
        public string? SubmittedValue { get; set; }
        public List<string> SubmittedValues { get; set; } = new();
    }

    public class FormFillViewModel
    {
        public int FormId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsAcceptingResponses { get; set; } = true;
        public bool IsAnonymous { get; set; }
        public List<FormFieldFillViewModel> Fields { get; set; } = new();

        /// <summary>Round-tripped from the UId query parameter on the public /Formular link; null for the internal, logged-in Fill page.</summary>
        public Guid? PersonPublicId { get; set; }

        /// <summary>Form.PublicId — used as the Id route value when posting back from the public /Formular link.</summary>
        public Guid FormPublicId { get; set; }
    }

    // ── Offentligt link (Formular) ──────────────────────────────────────────

    /// <summary>
    /// Result of resolving GET/POST /Formular?Id=..&amp;UId=.. — the gate outcome plus (when
    /// available) the form to render, so the same view can show the fill-out form, a closed/error
    /// message, or the "already answered" notice.
    /// </summary>
    public class PublicFormAccessViewModel
    {
        public PublicFormStatus Status { get; set; }
        public FormFillViewModel? Form { get; set; }

        /// <summary>True once the POST has been saved successfully — the view then shows a thank-you message instead of the field list.</summary>
        public bool Submitted { get; set; }

        public Dictionary<int, string> FieldErrors { get; set; } = new();
    }

    public class FormAnswerInputViewModel
    {
        public int FormFieldId { get; set; }
        public string? Value { get; set; }
        public List<string> Values { get; set; } = new();
    }

    // ── Svar (Responses) ───────────────────────────────────────────────────

    public class FormResponseColumnViewModel
    {
        public int FormFieldId { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class FormResponseRowViewModel
    {
        public int SubmissionId { get; set; }
        public string SubmittedByDisplayName { get; set; } = string.Empty;
        public DateTime SubmittedAtUtc { get; set; }

        /// <summary>Keyed by FormFieldId.</summary>
        public Dictionary<int, string> Answers { get; set; } = new();
    }

    public class FormResponsesViewModel
    {
        public int FormId { get; set; }
        public string FormTitle { get; set; } = string.Empty;
        public List<FormResponseColumnViewModel> Columns { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public List<FormResponseRowViewModel> Rows { get; set; } = new();

        public int VersionNumber { get; set; }
        public int VersionCount { get; set; }
        public bool IsLatestVersion { get; set; } = true;
        public int? CurrentVersionId { get; set; }
        public int? PreviousVersionId { get; set; }
        public int? NextVersionId { get; set; }
    }
}
