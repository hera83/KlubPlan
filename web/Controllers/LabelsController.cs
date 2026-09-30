using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Entities;
using web.Infrastructure;
using web.Infrastructure.Labels;
using web.Repositories.LabelCollections;
using web.Repositories.LabelCollections.Dtos;
using web.Repositories.LabelCollections.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    /// <summary>
    /// Værktøjer → Labels: the table of label collections (create, print, delete) and the label designer
    /// (text, løbenummer, images and QR codes on the collection's A4 sheet grid) with its image library.
    /// </summary>
    [Authorize]
    public class LabelsController : Controller
    {
        private readonly ILabelCollectionService _collectionService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LabelsController(ILabelCollectionService collectionService, UserManager<ApplicationUser> userManager)
        {
            _collectionService = collectionService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var model = await _collectionService.GetCollectionsAsync(new LabelCollectionFilterViewModel(), ct);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CollectionsTable(LabelCollectionFilterViewModel filter, CancellationToken ct)
        {
            var model = await _collectionService.GetCollectionsAsync(filter, ct);
            return PartialView("_LabelCollectionsTableBody", model);
        }

        /// <summary>"Opret label-samling" → Gem: creates the collection and sends the browser on to its designer.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LabelCollectionCreateViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson(FirstModelError() ?? "Label-samlingen kunne ikke oprettes.");

            var result = await _collectionService.CreateAsync(model, ct);
            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage!);

            // The toast is shown on the designer page after the redirect.
            this.ToastSuccess(result.Message!);
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Designer), new { id = result.Id }) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var deleted = await _collectionService.DeleteAsync(id, ct);
            return deleted
                ? this.ToastSuccessJson("Label-samlingen er slettet.")
                : this.ToastErrorJson("Label-samlingen blev ikke fundet.");
        }

        // ─── Designer ────────────────────────────────────────────────────────────

        public async Task<IActionResult> Designer(int id, CancellationToken ct)
        {
            var model = await _collectionService.GetDesignerAsync(id, ct);
            return model is null ? NotFound() : View(model);
        }

        /// <summary>The saved design (JSON) — the table's "Print labels" draws the first sheet from it.</summary>
        [HttpGet]
        public async Task<IActionResult> Design(int id, CancellationToken ct)
        {
            var model = await _collectionService.GetDesignerAsync(id, ct);
            if (model is null)
                return this.ToastErrorJson("Label-samlingen blev ikke fundet.");
            return Content(JsonSerializer.Serialize(new { success = true, design = model.Design }, LabelCollectionService.JsonOptions), "application/json");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDesign(LabelDesignSaveViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson(FirstModelError() ?? "Label-samlingen kunne ikke gemmes.");

            LabelCollectionDesignDto? design;
            try
            {
                design = JsonSerializer.Deserialize<LabelCollectionDesignDto>(model.DesignJson, LabelCollectionService.JsonOptions);
            }
            catch (JsonException)
            {
                design = null;
            }
            if (design is null)
                return this.ToastErrorJson("Label-samlingen kunne ikke gemmes (ugyldige data).");

            var result = await _collectionService.SaveDesignAsync(model.Id, design, ct);
            return result.Success ? this.ToastSuccessJson(result.Message!) : this.ToastErrorJson(result.ErrorMessage!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(LabelDesignRules.MaxImageBytes + 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = LabelDesignRules.MaxImageBytes + 1024 * 1024)]
        public async Task<IActionResult> UploadMedia(int id, IFormFile? file, CancellationToken ct)
        {
            if (file is null || file.Length == 0)
                return this.ToastErrorJson("Vælg en fil.");

            await using var stream = file.OpenReadStream();
            var result = await _collectionService.UploadMediaAsync(id, stream, file.FileName, file.Length, _userManager.GetUserId(User), ct);
            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage!);
            return Json(new { success = true, message = result.Message, type = "success", media = result.Media });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedia(int id, Guid mediaId, CancellationToken ct)
        {
            var result = await _collectionService.DeleteMediaAsync(id, mediaId, ct);
            return result.Success ? this.ToastSuccessJson(result.Message!) : this.ToastErrorJson(result.ErrorMessage!);
        }

        /// <summary>An image from a collection's image library.</summary>
        [HttpGet]
        public async Task<IActionResult> Media(Guid id, CancellationToken ct)
        {
            var file = await _collectionService.GetMediaFileAsync(id, ct);
            if (file is null)
                return NotFound();

            // A media id always points at the same file, so the browser may cache it for a long time.
            Response.Headers.CacheControl = "private, max-age=604800";
            return PhysicalFile(file.Value.FullPath, file.Value.ContentType);
        }

        /// <summary>A QR code as SVG for a QR element in the designer — the PDF draws the same code (QrCodeSvg).</summary>
        [HttpGet]
        public IActionResult Qr(string? text, string? color)
        {
            text = (text ?? string.Empty).Trim();
            if (text.Length == 0 || text.Length > LabelDesignRules.MaxQrTextLength)
                return BadRequest();

            var dark = color is not null && Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$") ? color : "#000000";
            // Same text + color always gives the same code.
            Response.Headers.CacheControl = "private, max-age=604800";
            return Content(QrCodeSvg.Create(text, dark), "image/svg+xml");
        }

        /// <summary>The collection's label sheets as a PDF, shown in the browser (new tab) so it can be printed from there.</summary>
        [HttpGet]
        public async Task<IActionResult> Pdf(LabelDesignPrintViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                this.ToastError(FirstModelError() ?? "Labels kunne ikke printes.");
                return RedirectToAction(nameof(Index));
            }

            var pdf = await _collectionService.BuildPdfAsync(model.Id, model.CutMarks, ct);
            if (pdf is null)
                return NotFound();
            if (!pdf.Success)
            {
                this.ToastWarning(pdf.ErrorMessage!);
                return RedirectToAction(nameof(Designer), new { id = model.Id });
            }

            return this.InlineLabelPdf(pdf);
        }

        private string? FirstModelError()
            => ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
    }
}
