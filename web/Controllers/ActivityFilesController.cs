using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using web.Constants;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.ActivityFiles.Dtos;
using web.Repositories.ActivityFiles.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    /// <summary>
    /// The "Filer" tab on an activity (document hotel). All administrators have access — there is no
    /// separate access level per activity. The tab UI lives in Views/Activities/_Files*.cshtml and
    /// wwwroot/js/activity-files.js.
    /// </summary>
    [Authorize(Policy = "AdminOrDeveloper")]
    public class ActivityFilesController : Controller
    {
        private const string PanelView = "~/Views/Activities/_FilesPanel.cshtml";
        private const string VersionsView = "~/Views/Activities/_FileVersionsBody.cshtml";

        private readonly IActivityFileService _fileService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public ActivityFilesController(IActivityFileService fileService, UserManager<ApplicationUser> userManager, IWebHostEnvironment env, IConfiguration config)
        {
            _fileService = fileService;
            _userManager = userManager;
            _env = env;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> Panel(int activityId, int? folderId, string? search, CancellationToken ct)
        {
            var model = await _fileService.GetFolderAsync(activityId, folderId, search, ct);
            return model is null ? NotFound() : PartialView(PanelView, model);
        }

        [HttpGet]
        public async Task<IActionResult> FolderTree(int activityId, CancellationToken ct)
        {
            var tree = await _fileService.GetFolderTreeAsync(activityId, ct);
            return Json(tree);
        }

        [HttpGet]
        public async Task<IActionResult> Versions(int activityId, int fileId, CancellationToken ct)
        {
            var model = await _fileService.GetVersionsAsync(activityId, fileId, ct);
            return model is null ? NotFound() : PartialView(VersionsView, model);
        }

        [HttpGet]
        public async Task<IActionResult> Download(int activityId, int fileId, int? versionId, CancellationToken ct)
        {
            var file = await _fileService.GetDownloadAsync(activityId, fileId, versionId, ct);
            if (file is null)
                return NotFound();

            return PhysicalFile(file.FullPath, file.ContentType, file.FileName, enableRangeProcessing: true);
        }

        /// <summary>Shows the file in the browser (new tab) when it can — otherwise falls back to a normal download.</summary>
        [HttpGet]
        public async Task<IActionResult> Open(int activityId, int fileId, int? versionId, CancellationToken ct)
        {
            var file = await _fileService.GetDownloadAsync(activityId, fileId, versionId, ct);
            if (file is null)
                return NotFound();

            if (!file.CanPreview)
                return PhysicalFile(file.FullPath, file.ContentType, file.FileName, enableRangeProcessing: true);

            var disposition = new ContentDispositionHeaderValue("inline");
            disposition.SetHttpFileName(file.FileName);
            Response.Headers[HeaderNames.ContentDisposition] = disposition.ToString();
            Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
            return PhysicalFile(file.FullPath, file.ContentType, enableRangeProcessing: true);
        }

        /// <summary>Selected files/folders (current version of each file) as one zip.</summary>
        [HttpGet]
        public async Task<IActionResult> DownloadZip(ActivityFilesSelectionViewModel selection, CancellationToken ct)
        {
            var zip = await _fileService.GetZipAsync(selection.ActivityId, selection.FileIds, selection.FolderIds, ct);
            if (zip is null)
                return NotFound();

            var tempDir = Path.Combine(_env.ContentRootPath, _config["AppSettings:FilesPath"] ?? "App_files", FileCategories.Temp);
            Directory.CreateDirectory(tempDir);
            var tempPath = Path.Combine(tempDir, $"{Guid.NewGuid()}.zip");

            using (var zipStream = new FileStream(tempPath, FileMode.CreateNew))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                foreach (var folder in zip.EmptyFolders)
                {
                    archive.CreateEntry(folder);
                }
                foreach (var entry in zip.Entries)
                {
                    archive.CreateEntryFromFile(entry.FullPath, entry.EntryPath, CompressionLevel.Fastest);
                }
            }

            // DeleteOnClose removes the temp zip as soon as the response has been sent.
            var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 81920, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
            return File(stream, "application/zip", zip.ZipFileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(ActivityFileRules.MaxRequestSizeBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = ActivityFileRules.MaxRequestSizeBytes)]
        public async Task<IActionResult> Upload(ActivityFileUploadViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid || model.File is null)
                return this.ToastErrorJson("Filen kunne ikke uploades.");

            await using var stream = model.File.OpenReadStream();
            var result = await _fileService.UploadAsync(new UploadActivityFileRequestDto
            {
                ActivityId = model.ActivityId,
                FolderId = model.FolderId,
                RelativePath = model.RelativePath,
                TargetFileId = model.TargetFileId,
                FileName = model.File.FileName,
                Length = model.File.Length,
                Content = stream,
                UserId = _userManager.GetUserId(User)
            }, ct);

            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Filen kunne ikke uploades.");

            return Json(new
            {
                success = true,
                type = "success",
                message = result.IsNewVersion
                    ? $"{result.FileName} er gemt som version {result.VersionNumber}."
                    : $"{result.FileName} er uploadet.",
                fileName = result.FileName,
                versionNumber = result.VersionNumber,
                isNewVersion = result.IsNewVersion
            });
        }

        /// <summary>Creates a folder (FolderId empty) or renames one (FolderId set).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFolder(ActivityFolderFormViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson(FirstModelError() ?? "Mappen kunne ikke gemmes.");

            var result = model.FolderId.HasValue
                ? await _fileService.RenameFolderAsync(model.ActivityId, model.FolderId.Value, model.Name, ct)
                : await _fileService.CreateFolderAsync(model.ActivityId, model.ParentFolderId, model.Name, _userManager.GetUserId(User), ct);

            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Mappen kunne ikke gemmes.");

            return Json(new { success = true, type = "success", message = model.FolderId.HasValue ? "Mappen er omdøbt." : "Mappen er oprettet.", id = result.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameFile(ActivityFileRenameViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson(FirstModelError() ?? "Filen kunne ikke omdøbes.");

            var result = await _fileService.RenameFileAsync(model.ActivityId, model.FileId, model.Name, ct);
            return result.Success
                ? this.ToastSuccessJson("Filen er omdøbt.")
                : this.ToastErrorJson(result.ErrorMessage ?? "Filen kunne ikke omdøbes.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Move(ActivityFilesSelectionViewModel selection, CancellationToken ct)
        {
            if (!ModelState.IsValid || selection.IsEmpty)
                return this.ToastErrorJson("Vælg mindst én fil eller mappe.");

            var result = await _fileService.MoveAsync(selection.ActivityId, selection.FileIds, selection.FolderIds, selection.TargetFolderId, ct);

            if (result.Skipped.Count == 0)
            {
                return result.MovedCount == 0
                    ? this.ToastJson(true, "Elementerne ligger allerede i den mappe.", "info")
                    : this.ToastSuccessJson(result.MovedCount == 1 ? "1 element er flyttet." : $"{result.MovedCount} elementer er flyttet.");
            }

            var skipped = string.Join(", ", result.Skipped);
            return result.MovedCount == 0
                ? this.ToastErrorJson($"Intet blev flyttet: {skipped}.")
                : this.ToastWarningJson($"{result.MovedCount} flyttet. Ikke flyttet: {skipped}.");
        }

        /// <summary>Counts for the delete confirmation modal (files, folders, versions and size).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSummary(ActivityFilesSelectionViewModel selection, CancellationToken ct)
        {
            var summary = await _fileService.GetDeleteSummaryAsync(selection.ActivityId, selection.FileIds, selection.FolderIds, ct);
            return Json(new
            {
                folderCount = summary.FolderCount,
                fileCount = summary.FileCount,
                versionCount = summary.VersionCount,
                totalBytes = summary.TotalBytes,
                totalSizeText = ByteSizeFormatter.FormatDanish(summary.TotalBytes)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(ActivityFilesSelectionViewModel selection, CancellationToken ct)
        {
            if (!ModelState.IsValid || selection.IsEmpty)
                return this.ToastErrorJson("Vælg mindst én fil eller mappe.");

            var summary = await _fileService.DeleteAsync(selection.ActivityId, selection.FileIds, selection.FolderIds, ct);
            if (summary.FileCount == 0 && summary.FolderCount == 0)
                return this.ToastErrorJson("Elementerne blev ikke fundet.");

            var parts = new List<string>();
            if (summary.FolderCount > 0)
                parts.Add(summary.FolderCount == 1 ? "1 mappe" : $"{summary.FolderCount} mapper");
            if (summary.FileCount > 0 || summary.FolderCount == 0)
                parts.Add(summary.FileCount == 1 ? "1 fil" : $"{summary.FileCount} filer");

            return this.ToastSuccessJson($"{string.Join(" og ", parts)} er slettet ({ByteSizeFormatter.FormatDanish(summary.TotalBytes)} frigjort).");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreVersion(int activityId, int versionId, CancellationToken ct)
        {
            var result = await _fileService.RestoreVersionAsync(activityId, versionId, _userManager.GetUserId(User), ct);
            return result.Success
                ? this.ToastSuccessJson($"Versionen er gendannet som version {result.Id}.")
                : this.ToastErrorJson(result.ErrorMessage ?? "Versionen kunne ikke gendannes.");
        }

        private string? FirstModelError()
            => ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
    }
}
