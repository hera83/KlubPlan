using System.ComponentModel.DataAnnotations;
using web.Constants;
using web.Repositories.InfoScreens.Dtos;

namespace web.ViewModels
{
    // ── Tabel ───────────────────────────────────────────────────────────────

    public class InfoScreenFilterViewModel
    {
        public string? SearchText { get; set; }

        /// <summary>"active", "inactive" or empty for all.</summary>
        public string? Status { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public List<InfoScreenListItemViewModel> Screens { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class InfoScreenListItemViewModel
    {
        public int Id { get; set; }
        public Guid PublicId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public int SlideCount { get; set; }

        /// <summary>Slides that are played (not hidden).</summary>
        public int VisibleSlideCount { get; set; }

        /// <summary>One round of the slideshow (visible slides).</summary>
        public int TotalSeconds { get; set; }

        public DateTime ChangedAtUtc { get; set; }

        /// <summary>"2 min 30 sek" — one round of the slideshow.</summary>
        public string TotalDurationText => FormatDuration(TotalSeconds);

        public static string FormatDuration(int seconds)
        {
            if (seconds <= 0)
                return "–";
            var minutes = seconds / 60;
            var rest = seconds % 60;
            if (minutes == 0)
                return $"{rest} sek";
            return rest == 0 ? $"{minutes} min" : $"{minutes} min {rest} sek";
        }
    }

    /// <summary>"Opret infoskærm" modal: title and active/passive — then on to the designer.</summary>
    public class InfoScreenCreateViewModel
    {
        [Required(ErrorMessage = "Titel skal udfyldes.")]
        [StringLength(InfoScreenRules.MaxTitleLength, ErrorMessage = "Titlen må højst være {1} tegn.")]
        public string Title { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    // ── Designer ────────────────────────────────────────────────────────────

    public class InfoScreenDesignerViewModel
    {
        public int Id { get; set; }
        public Guid PublicId { get; set; }

        /// <summary>The screen's settings and slides — the designer's starting state.</summary>
        public InfoScreenDesignDto Design { get; set; } = new();

        /// <summary>The screen's media library.</summary>
        public List<InfoScreenMediaDto> Media { get; set; } = new();
    }

    /// <summary>Designer → Gem: the whole design as JSON (InfoScreenDesignDto).</summary>
    public class InfoScreenDesignSaveViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Der er intet at gemme.")]
        public string DesignJson { get; set; } = string.Empty;
    }

    // ── Offentlig visning ───────────────────────────────────────────────────

    /// <summary>/Infoskaerm?Id={PublicId}: the player page. It loads the slides itself (Data) and keeps them up to date.</summary>
    public class InfoScreenPlayerPageViewModel
    {
        public Guid PublicId { get; set; }
        public string Title { get; set; } = string.Empty;

        /// <summary>Plays even when inactive and includes a "Forhåndsvisning" badge — only for logged-in users.</summary>
        public bool Preview { get; set; }

        /// <summary>1-based slide to start on (the designer's "Afspil fra denne side").</summary>
        public int StartSlide { get; set; } = 1;
    }
}
