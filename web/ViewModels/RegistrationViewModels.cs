using System.ComponentModel.DataAnnotations;
using web.Constants;

namespace web.ViewModels
{
    // ── Liste (Index) ──────────────────────────────────────────────────────

    public class ArrangementListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        /// <summary>Earliest shift start time; null when the arrangement has no shifts yet.</summary>
        public DateTime? EventDateUtc { get; set; }

        public int RegistrationCount { get; set; }

        /// <summary>Sum of NeededCount across all shifts, minus RegistrationCount — how many pladser der stadig mangler at blive besat.</summary>
        public int MissingCount { get; set; }

        public DateTime? RegistrationOpensAtUtc { get; set; }
        public DateTime? RegistrationClosesAtUtc { get; set; }

        /// <summary>True = temporarily forced open regardless of the dates above.</summary>
        public bool RegistrationForcedOpen { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public class ArrangementFilterViewModel
    {
        public string? SearchText { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<ArrangementListItemViewModel> Arrangementer { get; set; } = new();
        public int TotalCount { get; set; }
    }

    // ── Builder (Opret/Rediger) ─────────────────────────────────────────────

    public class ArrangementFormFieldBuilderViewModel
    {
        /// <summary>0 for a field that does not exist in the database yet.</summary>
        public int Id { get; set; }

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

    public class ArrangementShiftRequirementBuilderViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kravet skal have en tekst")]
        [StringLength(500)]
        public string Text { get; set; } = string.Empty;

        public int Order { get; set; }
    }

    public class ArrangementShiftBuilderViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vagten skal have en titel")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Starttidspunkt er påkrævet")]
        public DateTime Start { get; set; }

        [Required(ErrorMessage = "Sluttidspunkt er påkrævet")]
        public DateTime End { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [Range(1, 1000, ErrorMessage = "Antal skal være mindst 1")]
        public int NeededCount { get; set; } = 1;

        public int Order { get; set; }

        public List<ArrangementShiftRequirementBuilderViewModel> Requirements { get; set; } = new();
    }

    public class ArrangementPersonOptionViewModel
    {
        public int Id { get; set; }
        public string Uid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ArrangementBuilderViewModel
    {
        /// <summary>0 (eller null på GET-routen) betyder et nyt arrangement.</summary>
        public int Id { get; set; }

        [Required(ErrorMessage = "Arrangementet skal have en titel")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        // a) Tilmeldingsformular
        public List<ArrangementFormFieldBuilderViewModel> FormFields { get; set; } = new();

        // b) + c) Vagter, med krav pr. vagt
        public List<ArrangementShiftBuilderViewModel> Shifts { get; set; } = new();

        // d) Detaljer
        public DateTime? RegistrationOpensAt { get; set; }
        public DateTime? RegistrationClosesAt { get; set; }

        // e) Adgang
        public ArrangementAccessMode AccessMode { get; set; } = ArrangementAccessMode.Open;
        public List<int> AllowedPersonIds { get; set; } = new();
        public List<int> AllowedGroupIds { get; set; } = new();

        /// <summary>Server-populerede options til Adgang-fanen — postes ikke tilbage fra klienten.</summary>
        public List<PersonGroupOptionViewModel> GroupOptions { get; set; } = new();

        /// <summary>Server-populerede options til Adgang-fanen — postes ikke tilbage fra klienten.</summary>
        public List<ArrangementPersonOptionViewModel> PersonOptions { get; set; } = new();
    }
}
