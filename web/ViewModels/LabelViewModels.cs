using System.ComponentModel.DataAnnotations;
using System.Globalization;
using web.Constants;
using web.Repositories.LabelCollections.Dtos;

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

        /// <summary>Different label designs in the collection.</summary>
        public int LabelCount { get; set; }

        /// <summary>Labels on the sheets, i.e. the sum of the quantities.</summary>
        public int Copies { get; set; }

        public int Across { get; set; }
        public int Down { get; set; }
        public bool Landscape { get; set; }

        public DateTime ChangedAtUtc { get; set; }

        /// <summary>A4 sheets needed for all copies.</summary>
        public int Sheets => Copies == 0 ? 0 : (int)Math.Ceiling(Copies / (double)(Across * Down));

        /// <summary>"70 × 37,1 mm" — one label on the collection's sheet grid.</summary>
        public string LabelSizeText => FormatLabelSize(Across, Down, Landscape);

        public static string FormatLabelSize(int across, int down, bool landscape)
        {
            var (width, height) = LabelDesignRules.LabelSizeMm(across, down, landscape);
            var da = CultureInfo.GetCultureInfo("da-DK");
            return $"{width.ToString("0.#", da)} × {height.ToString("0.#", da)} mm";
        }
    }

    /// <summary>"Opret label-samling" modal: name and sheet grid — then on to the designer.</summary>
    public class LabelCollectionCreateViewModel
    {
        [Required(ErrorMessage = "Navn skal udfyldes.")]
        [StringLength(LabelRules.MaxCollectionNameLength, ErrorMessage = "Navnet må højst være {1} tegn.")]
        public string Name { get; set; } = string.Empty;

        [Range(1, LabelRules.MaxAcross, ErrorMessage = "Labels i bredden skal være mellem {1} og {2}.")]
        public int Across { get; set; } = LabelDesignRules.DefaultAcross;

        [Range(1, LabelRules.MaxDown, ErrorMessage = "Labels i højden skal være mellem {1} og {2}.")]
        public int Down { get; set; } = LabelDesignRules.DefaultDown;

        public bool Landscape { get; set; }
    }

    // ── Label-designer ──────────────────────────────────────────────────────

    public class LabelDesignerViewModel
    {
        public int Id { get; set; }

        /// <summary>The collection's name, sheet grid and labels — the designer's starting state.</summary>
        public LabelCollectionDesignDto Design { get; set; } = new();

        /// <summary>The collection's image library.</summary>
        public List<LabelMediaDto> Media { get; set; } = new();
    }

    /// <summary>Designer → Gem: the whole design as JSON (LabelCollectionDesignDto).</summary>
    public class LabelDesignSaveViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Der er intet at gemme.")]
        public string DesignJson { get; set; } = string.Empty;
    }

    /// <summary>"Print labels" for a designed collection: the grid is the collection's own, so only the cut marks are chosen.</summary>
    public class LabelDesignPrintViewModel
    {
        [Required]
        public int Id { get; set; }

        /// <summary>Short cut marks on the lines between labels, for cutting uncut full-sheet label paper with a paper cutter.</summary>
        public bool CutMarks { get; set; }
    }
}
