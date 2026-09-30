using System.ComponentModel.DataAnnotations;
using web.Constants;

namespace web.ViewModels
{
    // ── Fælles for alle labels ──────────────────────────────────────────────

    /// <summary>One row in the shared label editor (Views/Shared/Partials/_LabelRowsEditor.cshtml).</summary>
    public class LabelInputViewModel
    {
        [StringLength(LabelRules.MaxTextLength, ErrorMessage = "Teksten på en label må højst være {1} tegn.")]
        public string? Text { get; set; }

        [Range(1, LabelRules.MaxQuantity, ErrorMessage = "Antal skal være mellem {1} og {2}.")]
        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// "Print labels": the A4 sheet's grid (labels across × down, portrait or landscape) and cut marks —
    /// the fields of the shared print modal (Views/Shared/Partials/_PrintLabelsModal.cshtml).
    /// </summary>
    public class LabelSheetViewModel
    {
        [Range(1, LabelRules.MaxAcross, ErrorMessage = "Labels i bredden skal være mellem {1} og {2}.")]
        public int Across { get; set; } = 3;

        [Range(1, LabelRules.MaxDown, ErrorMessage = "Labels i højden skal være mellem {1} og {2}.")]
        public int Down { get; set; } = 8;

        public bool Landscape { get; set; }

        /// <summary>Short cut marks on the lines between labels, for cutting uncut full-sheet label paper with a paper cutter.</summary>
        public bool CutMarks { get; set; }
    }

    /// <summary>Options for the shared "Print labels" modal.</summary>
    public class PrintLabelsModalViewModel
    {
        /// <summary>The modal's element id (also prefixes its field ids).</summary>
        public string ModalId { get; set; } = "printLabelsModal";

        /// <summary>Shows "Labels fra: Hele listen / Kun linjer i nuværende filter" (work lists).</summary>
        public bool ShowFilterScope { get; set; }
    }

    // ── Labels (Værktøjer) ──────────────────────────────────────────────────

    public class LabelCollectionFilterViewModel
    {
        public string? SearchText { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public List<LabelCollectionListItemViewModel> Collections { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class LabelCollectionListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>Different labels in the collection.</summary>
        public int LabelCount { get; set; }

        /// <summary>Labels on the sheets, i.e. the sum of the quantities.</summary>
        public int Copies { get; set; }

        public DateTime ChangedAtUtc { get; set; }
    }

    /// <summary>"Opret/Rediger label-samling" modal — name plus all its labels, saved at once. No Id = create.</summary>
    public class LabelCollectionEditViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Navn skal udfyldes.")]
        [StringLength(LabelRules.MaxCollectionNameLength, ErrorMessage = "Navnet må højst være {1} tegn.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(LabelRules.MaxLabelsPerCollection, ErrorMessage = "En samling kan højst have {1} labels.")]
        public List<LabelInputViewModel> Labels { get; set; } = new();
    }
}
