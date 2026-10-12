using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Models;
using web.Extensions;
using web.Managers.InfoScreens.Dtos;
using web.Managers.InfoScreens.Interfaces;
using web.Repositories.InfoScreens.Dtos;
using web.Repositories.InfoScreens.Interfaces;
using web.Repositories.InfoScreens.Mappers;
using web.ViewModels;
using web.ViewModels.Mappers;

namespace web.Controllers;

/// <summary>
/// Værktøjer → Infoskærme: the table of screens (create, activate/deactivate, copy link, delete)
/// and the slide designer. The running screen itself is the public InfoskaermController.
/// </summary>
[Authorize]
public class InfoScreensController(
    IInfoScreenRepository infoScreenRepository,
    IInfoScreenManager infoScreenManager,
    UserManager<ApplicationUser> userManager) : Controller
{
    private const string NotFoundMessage = "Infoskærmen blev ikke fundet.";

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var filter = new InfoScreenFilterViewModel();
        var page = await infoScreenRepository.SearchAsync(filter.ToSearchInfoScreensDto(), cancellationToken);
        return View(page.ToInfoScreenFilterViewModel(filter));
    }

    [HttpGet]
    public async Task<IActionResult> ScreensTable(InfoScreenFilterViewModel filter, CancellationToken cancellationToken)
    {
        var page = await infoScreenRepository.SearchAsync(filter.ToSearchInfoScreensDto(), cancellationToken);
        return PartialView("_InfoScreensTableBody", page.ToInfoScreenFilterViewModel(filter));
    }

    /// <summary>"Opret infoskærm" → Gem: creates the screen and sends the browser on to its designer.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InfoScreenCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson(FirstModelError() ?? "Infoskærmen kunne ikke oprettes.");

        var result = await infoScreenRepository.CreateAsync(model.ToCreateInfoScreenDto(), cancellationToken);
        if (result.Status != SaveInfoScreenStatus.Saved)
            return this.ToastErrorJson(result.ErrorMessage ?? "Infoskærmen kunne ikke oprettes.");

        // The toast is shown on the designer page after the redirect.
        this.ToastSuccess($"Infoskærmen \"{result.Title}\" er oprettet.");
        return Json(new { success = true, redirectUrl = Url.Action(nameof(Designer), new { id = result.Id }) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool isActive, CancellationToken cancellationToken)
    {
        var result = await infoScreenRepository.SetActiveAsync(id, isActive, cancellationToken);
        if (result.Status != SaveInfoScreenStatus.Saved)
            return this.ToastErrorJson(NotFoundMessage);

        return this.ToastSuccessJson(isActive
            ? $"Infoskærmen \"{result.Title}\" er aktiveret."
            : $"Infoskærmen \"{result.Title}\" er deaktiveret.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await infoScreenManager.DeleteAsync(id, cancellationToken)
            ? this.ToastSuccessJson("Infoskærmen er slettet.")
            : this.ToastErrorJson(NotFoundMessage);

    // ─── Designer ────────────────────────────────────────────────────────────

    public async Task<IActionResult> Designer(int id, CancellationToken cancellationToken)
    {
        var designer = await infoScreenRepository.GetDesignerAsync(id, cancellationToken);
        return designer is null ? NotFound() : View(designer.ToInfoScreenDesignerViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDesign(InfoScreenDesignSaveViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson(FirstModelError() ?? "Infoskærmen kunne ikke gemmes.");

        InfoScreenDesignDto? design;
        try
        {
            design = JsonSerializer.Deserialize<InfoScreenDesignDto>(model.DesignJson, InfoScreenMapper.JsonOptions);
        }
        catch (JsonException)
        {
            design = null;
        }
        if (design is null)
            return this.ToastErrorJson("Infoskærmen kunne ikke gemmes (ugyldige data).");

        var result = await infoScreenRepository.SaveDesignAsync(model.Id, design, cancellationToken);
        return result.Status switch
        {
            SaveInfoScreenStatus.Saved => this.ToastSuccessJson($"Infoskærmen \"{result.Title}\" er gemt."),
            SaveInfoScreenStatus.NotFound => this.ToastErrorJson(NotFoundMessage),
            _ => this.ToastErrorJson(result.ErrorMessage!)
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(InfoScreenRules.MaxVideoBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = InfoScreenRules.MaxVideoBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadMedia(int id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return this.ToastErrorJson("Vælg en fil.");

        await using var stream = file.OpenReadStream();
        var result = await infoScreenManager.UploadMediaAsync(id, stream, file.FileName, file.Length, userManager.GetUserId(User), cancellationToken);
        return result.Status switch
        {
            UploadInfoScreenMediaStatus.Uploaded => Json(new { success = true, message = $"\"{result.Media!.Name}\" er uploadet.", type = "success", media = result.Media }),
            UploadInfoScreenMediaStatus.NotFound => this.ToastErrorJson(NotFoundMessage),
            _ => this.ToastErrorJson(result.ErrorMessage!)
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMedia(int id, Guid mediaId, CancellationToken cancellationToken) =>
        await infoScreenManager.DeleteMediaAsync(id, mediaId, cancellationToken) switch
        {
            RemoveInfoScreenMediaStatus.Removed => this.ToastSuccessJson("Filen er slettet."),
            RemoveInfoScreenMediaStatus.InUse => this.ToastErrorJson("Filen bruges på en gemt side. Fjern den fra siden og gem først."),
            _ => this.ToastErrorJson("Filen blev ikke fundet.")
        };

    private string? FirstModelError()
        => ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
}
