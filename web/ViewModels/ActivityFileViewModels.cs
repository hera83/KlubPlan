using System.ComponentModel.DataAnnotations;
using web.Constants;

namespace web.ViewModels
{
    /// <summary>Content of one folder (or a search across all folders) in an activity's "Filer" tab.</summary>
    public class ActivityFilesViewModel
    {
        public int ActivityId { get; set; }

        /// <summary>Null = root level.</summary>
        public int? CurrentFolderId { get; set; }

        /// <summary>Root first, current folder last. The root crumb has Id null.</summary>
        public List<ActivityFolderBreadcrumbViewModel> Breadcrumbs { get; set; } = new();

        public List<ActivityFolderItemViewModel> Folders { get; set; } = new();

        public List<ActivityFileItemViewModel> Files { get; set; } = new();

        /// <summary>Set when the list is a search across all folders instead of one folder's content.</summary>
        public string? SearchText { get; set; }

        public bool IsSearch => !string.IsNullOrWhiteSpace(SearchText);

        /// <summary>Number of files in the whole activity — for the tab badge and footer.</summary>
        public int TotalFileCount { get; set; }

        /// <summary>Disk usage of the whole activity incl. all older versions.</summary>
        public long TotalSizeBytes { get; set; }
    }

    public class ActivityFolderBreadcrumbViewModel
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ActivityFolderItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>Direct children (sub folders + files).</summary>
        public int ItemCount { get; set; }

        public DateTime ModifiedAtUtc { get; set; }

        /// <summary>Folder path of the parent (only used in search results), e.g. "Planlægning / Budget".</summary>
        public string? ParentPath { get; set; }
    }

    public class ActivityFileItemViewModel
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string IconClass { get; set; } = "bi-file-earmark";
        public int? FolderId { get; set; }

        /// <summary>Folder path (only used in search results), e.g. "Planlægning / Budget". Empty = root.</summary>
        public string? FolderPath { get; set; }

        public int CurrentVersionId { get; set; }
        public int CurrentVersionNumber { get; set; }
        public int VersionCount { get; set; }
        public long SizeBytes { get; set; }
        public string ContentType { get; set; } = string.Empty;

        /// <summary>True when the browser can show the file itself (pdf, images, text, audio/video) — "Åbn" opens it in a new tab.</summary>
        public bool CanPreview { get; set; }

        public DateTime ModifiedAtUtc { get; set; }
        public string? ModifiedByName { get; set; }
    }

    /// <summary>Content of the "Versioner"-modal.</summary>
    public class ActivityFileVersionsViewModel
    {
        public int ActivityId { get; set; }
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string IconClass { get; set; } = "bi-file-earmark";
        public bool CanPreview { get; set; }
        public List<ActivityFileVersionItemViewModel> Versions { get; set; } = new();
    }

    public class ActivityFileVersionItemViewModel
    {
        public int Id { get; set; }
        public int VersionNumber { get; set; }
        public long SizeBytes { get; set; }
        public string? UploadedByName { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public bool IsCurrent { get; set; }
    }

    /// <summary>One entry in the "Flyt til…"-modal's folder picker (flattened tree).</summary>
    public class ActivityFolderTreeItemViewModel
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Depth { get; set; }
    }

    public class ActivityFolderFormViewModel
    {
        [Required]
        public int ActivityId { get; set; }

        /// <summary>Set when renaming an existing folder.</summary>
        public int? FolderId { get; set; }

        /// <summary>Parent for a new folder (null = root).</summary>
        public int? ParentFolderId { get; set; }

        [Required(ErrorMessage = "Angiv et navn.")]
        [StringLength(ActivityFileRules.MaxNameLength, ErrorMessage = "Navnet må højst være {1} tegn.")]
        public string Name { get; set; } = string.Empty;
    }

    public class ActivityFileRenameViewModel
    {
        [Required]
        public int ActivityId { get; set; }

        [Required]
        public int FileId { get; set; }

        [Required(ErrorMessage = "Angiv et navn.")]
        [StringLength(ActivityFileRules.MaxNameLength, ErrorMessage = "Navnet må højst være {1} tegn.")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>A multi-selection of files and folders (delete, move, download as zip).</summary>
    public class ActivityFilesSelectionViewModel
    {
        [Required]
        public int ActivityId { get; set; }

        public List<int> FileIds { get; set; } = new();

        public List<int> FolderIds { get; set; } = new();

        /// <summary>Only used by Move — null = root.</summary>
        public int? TargetFolderId { get; set; }

        public bool IsEmpty => FileIds.Count == 0 && FolderIds.Count == 0;
    }

    public class ActivityFileUploadViewModel
    {
        [Required]
        public int ActivityId { get; set; }

        /// <summary>Folder the upload was started in (null = root).</summary>
        public int? FolderId { get; set; }

        /// <summary>Path from a dropped/selected folder, e.g. "Billeder/2026/foto.jpg" — missing sub folders are created.</summary>
        [StringLength(1000)]
        public string? RelativePath { get; set; }

        /// <summary>Set by "Upload ny version" in the versions modal — the upload becomes a new version of this file regardless of its name.</summary>
        public int? TargetFileId { get; set; }

        [Required(ErrorMessage = "Vælg en fil.")]
        public IFormFile? File { get; set; }
    }
}
