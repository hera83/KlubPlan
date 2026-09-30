using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.InfoScreens;
using web.Repositories.InfoScreens.Dtos;
using web.Repositories.InfoScreens.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    /// <summary>
    /// Værktøjer → Infoskærme: the table of screens (create, activate/deactivate, copy link, delete)
    /// and the slide designer. The running screen itself is the public InfoskaermController.
    /// </summary>
    [Authorize]
    public class InfoScreensController : Controller
    {
        private readonly IInfoScreenService _screenService;
        private readonly UserManager<ApplicationUser> _userManager;

        public InfoScreensController(IInfoScreenService screenService, UserManager<ApplicationUser> userManager)
        {
            _screenService = screenService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var model = await _screenService.GetScreensAsync(new InfoScreenFilterViewModel(), ct);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ScreensTable(InfoScreenFilterViewModel filter, CancellationToken ct)
        {
            var model = await _screenService.GetScreensAsync(filter, ct);
            return PartialView("_InfoScreensTableBody", model);
        }

        /// <summary>"Opret infoskærm" → Gem: creates the screen and sends the browser on to its designer.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InfoScreenCreateViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson(FirstModelError() ?? "Infoskærmen kunne ikke oprettes.");

            var result = await _screenService.CreateAsync(model, ct);
            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage!);

            // The toast is shown on the designer page after the redirect.
            this.ToastSuccess(result.Message!);
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Designer), new { id = result.Id }) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActive(int id, bool isActive, CancellationToken ct)
        {
            var result = await _screenService.SetActiveAsync(id, isActive, ct);
            return result.Success ? this.ToastSuccessJson(result.Message!) : this.ToastErrorJson(result.ErrorMessage!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var deleted = await _screenService.DeleteAsync(id, ct);
            return deleted
                ? this.ToastSuccessJson("Infoskærmen er slettet.")
                : this.ToastErrorJson("Infoskærmen blev ikke fundet.");
        }

        // ─── Designer ────────────────────────────────────────────────────────────

        public async Task<IActionResult> Designer(int id, CancellationToken ct)
        {
            var model = await _screenService.GetDesignerAsync(id, ct);
            return model is null ? NotFound() : View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDesign(InfoScreenDesignSaveViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson(FirstModelError() ?? "Infoskærmen kunne ikke gemmes.");

            InfoScreenDesignDto? design;
            try
            {
                design = JsonSerializer.Deserialize<InfoScreenDesignDto>(model.DesignJson, InfoScreenService.JsonOptions);
            }
            catch (JsonException)
            {
                design = null;
            }
            if (design is null)
                return this.ToastErrorJson("Infoskærmen kunne ikke gemmes (ugyldige data).");

            var result = await _screenService.SaveDesignAsync(model.Id, design, ct);
            return result.Success ? this.ToastSuccessJson(result.Message!) : this.ToastErrorJson(result.ErrorMessage!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(InfoScreenRules.MaxVideoBytes + 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = InfoScreenRules.MaxVideoBytes + 1024 * 1024)]
        public async Task<IActionResult> UploadMedia(int id, IFormFile? file, CancellationToken ct)
        {
            if (file is null || file.Length == 0)
                return this.ToastErrorJson("Vælg en fil.");

            await using var stream = file.OpenReadStream();
            var result = await _screenService.UploadMediaAsync(id, stream, file.FileName, file.Length, _userManager.GetUserId(User), ct);
            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage!);
            return Json(new { success = true, message = result.Message, type = "success", media = result.Media });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedia(int id, Guid mediaId, CancellationToken ct)
        {
            var result = await _screenService.DeleteMediaAsync(id, mediaId, ct);
            return result.Success ? this.ToastSuccessJson(result.Message!) : this.ToastErrorJson(result.ErrorMessage!);
        }

        private string? FirstModelError()
            => ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
    }
}
