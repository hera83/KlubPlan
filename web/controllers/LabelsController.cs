using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Models;
using web.Extensions;
using web.Infrastructure.Labels;
using web.Managers.LabelCollections.Dtos;
using web.Managers.LabelCollections.Interfaces;
using web.Repositories.LabelCollections.Dtos;
using web.Repositories.LabelCollections.Interfaces;
using web.Repositories.LabelCollections.Mappers;
using web.ViewModels;
using web.ViewModels.Mappers;

namespace web.Controllers;

/// <summary>
/// Værktøjer → Labels: the table of label collections (create, print, delete) and the label designer
/// (text, løbenummer, images and QR codes on the collection's A4 sheet grid) with its image library.
/// </summary>
[Authorize]
public class LabelsController(
    ILabelCollectionRepository labelCollectionRepository,
    ILabelCollectionManager labelCollectionManager,
    UserManager<ApplicationUser> userManager) : Controller
{
    private const string NotFoundMessage = "Label-samlingen blev ikke fundet.";

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var filter = new LabelCollectionFilterViewModel();
        var page = await labelCollectionRepository.SearchAsync(filter.ToSearchLabelCollectionsDto(), cancellationToken);
        return View(page.ToLabelCollectionFilterViewModel(filter));
    }

    [HttpGet]
    public async Task<IActionResult> CollectionsTable(LabelCollectionFilterViewModel filter, CancellationToken cancellationToken)
    {
        var page = await labelCollectionRepository.SearchAsync(filter.ToSearchLabelCollectionsDto(), cancellationToken);
        return PartialView("_LabelCollectionsTableBody", page.ToLabelCollectionFilterViewModel(filter));
    }

    /// <summary>"Opret label-samling" → Gem: creates the collection and sends the browser on to its designer.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LabelCollectionCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson(FirstModelError() ?? "Label-samlingen kunne ikke oprettes.");

        var result = await labelCollectionRepository.CreateAsync(model.ToCreateLabelCollectionDto(), cancellationToken);
        if (result.Status != SaveLabelCollectionStatus.Saved)
            return this.ToastErrorJson(result.ErrorMessage ?? "Label-samlingen kunne ikke oprettes.");

        // The toast is shown on the designer page after the redirect.
        this.ToastSuccess($"Label-samlingen \"{result.Name}\" er oprettet.");
        return Json(new { success = true, redirectUrl = Url.Action(nameof(Designer), new { id = result.Id }) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await labelCollectionManager.DeleteAsync(id, cancellationToken)
            ? this.ToastSuccessJson("Label-samlingen er slettet.")
            : this.ToastErrorJson(NotFoundMessage);

    // ─── Designer ────────────────────────────────────────────────────────────

    public async Task<IActionResult> Designer(int id, CancellationToken cancellationToken)
    {
        var designer = await labelCollectionRepository.GetDesignerAsync(id, cancellationToken);
        return designer is null ? NotFound() : View(designer.ToLabelDesignerViewModel());
    }

    /// <summary>The saved design (JSON) — the table's "Print labels" draws the first sheet from it.</summary>
    [HttpGet]
    public async Task<IActionResult> Design(int id, CancellationToken cancellationToken)
    {
        var designer = await labelCollectionRepository.GetDesignerAsync(id, cancellationToken);
        if (designer is null)
            return this.ToastErrorJson(NotFoundMessage);
        return Content(JsonSerializer.Serialize(new { success = true, design = designer.Design }, LabelCollectionMapper.JsonOptions), "application/json");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDesign(LabelDesignSaveViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson(FirstModelError() ?? "Label-samlingen kunne ikke gemmes.");

        LabelCollectionDesignDto? design;
        try
        {
            design = JsonSerializer.Deserialize<LabelCollectionDesignDto>(model.DesignJson, LabelCollectionMapper.JsonOptions);
        }
        catch (JsonException)
        {
            design = null;
        }
        if (design is null)
            return this.ToastErrorJson("Label-samlingen kunne ikke gemmes (ugyldige data).");

        var result = await labelCollectionRepository.SaveDesignAsync(model.Id, design, cancellationToken);
        return result.Status switch
        {
            SaveLabelCollectionStatus.Saved => this.ToastSuccessJson($"Label-samlingen \"{result.Name}\" er gemt."),
            SaveLabelCollectionStatus.NotFound => this.ToastErrorJson(NotFoundMessage),
            _ => this.ToastErrorJson(result.ErrorMessage!)
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(LabelDesignRules.MaxImageBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = LabelDesignRules.MaxImageBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadMedia(int id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return this.ToastErrorJson("Vælg en fil.");

        await using var stream = file.OpenReadStream();
        var result = await labelCollectionManager.UploadMediaAsync(id, stream, file.FileName, file.Length, userManager.GetUserId(User), cancellationToken);
        return result.Status switch
        {
            UploadLabelMediaStatus.Uploaded => Json(new { success = true, message = $"\"{result.Media!.Name}\" er uploadet.", type = "success", media = result.Media }),
            UploadLabelMediaStatus.NotFound => this.ToastErrorJson(NotFoundMessage),
            _ => this.ToastErrorJson(result.ErrorMessage!)
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMedia(int id, Guid mediaId, CancellationToken cancellationToken) =>
        await labelCollectionManager.DeleteMediaAsync(id, mediaId, cancellationToken) switch
        {
            RemoveLabelCollectionMediaStatus.Removed => this.ToastSuccessJson("Billedet er slettet."),
            RemoveLabelCollectionMediaStatus.InUse => this.ToastErrorJson("Billedet bruges på en gemt label. Fjern det fra labelen og gem først."),
            _ => this.ToastErrorJson("Billedet blev ikke fundet.")
        };

    /// <summary>An image from a collection's image library.</summary>
    [HttpGet]
    public async Task<IActionResult> Media(Guid id, CancellationToken cancellationToken)
    {
        var file = await labelCollectionManager.GetMediaFileAsync(id, cancellationToken);
        if (file is null)
            return NotFound();

        // A media id always points at the same file, so the browser may cache it for a long time.
        Response.Headers.CacheControl = "private, max-age=604800";
        return PhysicalFile(file.FullPath, file.ContentType);
    }

    /// <summary>A QR code as SVG for a QR element in the designer — the PDF draws the same code (QrCodeHelper).</summary>
    [HttpGet]
    public IActionResult Qr(string? text, string? color)
    {
        text = (text ?? string.Empty).Trim();
        if (text.Length == 0 || text.Length > LabelDesignRules.MaxQrTextLength)
            return BadRequest();

        var dark = color is not null && Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$") ? color : "#000000";
        // Same text + color always gives the same code.
        Response.Headers.CacheControl = "private, max-age=604800";
        return Content(QrCodeHelper.Create(text, dark), "image/svg+xml");
    }

    /// <summary>The collection's label sheets as a PDF, shown in the browser (new tab) so it can be printed from there.</summary>
    [HttpGet]
    public async Task<IActionResult> Pdf(LabelDesignPrintViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            this.ToastError(FirstModelError() ?? "Labels kunne ikke printes.");
            return RedirectToAction(nameof(Index));
        }

        var pdf = await labelCollectionManager.BuildPdfAsync(model.Id, model.CutMarks, cancellationToken);
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
