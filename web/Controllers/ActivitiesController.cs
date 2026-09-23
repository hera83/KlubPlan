using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.Activities.Dtos;
using web.Repositories.Activities.Interfaces;
using web.Repositories.ActivityFiles.Interfaces;
using web.Repositories.ActivityLists.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    [Authorize]
    public class ActivitiesController : Controller
    {
        private readonly IActivityService _activityService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IActivityFileService _activityFileService;
        private readonly IActivityListService _activityListService;

        public ActivitiesController(IActivityService activityService, UserManager<ApplicationUser> userManager, IActivityFileService activityFileService, IActivityListService activityListService)
        {
            _activityListService = activityListService;
            _activityService = activityService;
            _activityFileService = activityFileService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var model = await _activityService.GetActivitiesAsync(new ActivityFilterViewModel(), ct);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ActivitiesTable(ActivityFilterViewModel filter, CancellationToken ct)
        {
            var model = await _activityService.GetActivitiesAsync(filter, ct);
            return PartialView("_ActivitiesTableBody", model);
        }

        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            return View("Form", await _activityService.GetFormShellAsync(ct));
        }

        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var model = await _activityService.GetActivityForEditAsync(id, ct);
            if (model is null)
            {
                this.ToastError("Aktiviteten blev ikke fundet.");
                return RedirectToAction(nameof(Index));
            }

            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Save(ActivityFormViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                this.ToastError("Aktiviteten kunne ikke gemmes. Kontroller felterne.");
                model.GroupOptions = await GetGroupOptionsAsync(ct);
                return View("Form", model);
            }

            var dto = new SaveActivityRequestDto
            {
                Id = model.Id,
                Title = model.Title,
                Description = model.Description,
                Location = model.Location,
                Category = model.Category,
                StartAt = model.StartAt,
                EndAt = model.EndAt,
                IsCancelled = model.IsCancelled,
                GroupIds = model.GroupIds,
                UserId = _userManager.GetUserId(User)
            };

            var result = await _activityService.SaveActivityAsync(dto, ct);
            if (!result.Success)
            {
                this.ToastError(result.ErrorMessage ?? "Aktiviteten kunne ikke gemmes.");
                model.GroupOptions = await GetGroupOptionsAsync(ct);
                return View("Form", model);
            }

            this.ToastSuccess(model.Id > 0 ? "Aktiviteten er opdateret." : "Aktiviteten er oprettet.");
            return RedirectToAction(nameof(Details), new { id = result.ActivityId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var success = await _activityService.DeleteActivityAsync(id, ct);
            return success
                ? this.ToastSuccessJson("Aktiviteten er slettet.")
                : this.ToastErrorJson("Aktiviteten blev ikke fundet.");
        }

        public async Task<IActionResult> Details(int id, string? tab, int? folder, CancellationToken ct)
        {
            var model = await _activityService.GetActivityDetailsAsync(id, ct);
            if (model is null)
            {
                this.ToastError("Aktiviteten blev ikke fundet.");
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.Developer))
            {
                model.Files = await _activityFileService.GetFolderAsync(id, folder, null, ct);
                model.Lists = await _activityListService.GetListsAsync(id, _userManager.GetUserId(User), ct);
            }

            ViewData["ActiveTab"] = string.IsNullOrWhiteSpace(tab) ? "oversigt" : tab;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> SaveWorkgroupMember(SaveWorkgroupMemberRequestDto dto, CancellationToken ct)
        {
            var result = await _activityService.SaveWorkgroupMemberAsync(dto, ct);
            if (!result.Success)
            {
                this.ToastError(result.ErrorMessage ?? "Medlemmet kunne ikke gemmes.");
            }
            else
            {
                this.ToastSuccess(dto.Id > 0 ? "Medlemmet er opdateret." : "Medlemmet er tilføjet.");
            }

            return RedirectToAction(nameof(Details), new { id = dto.ActivityId, tab = "arbejdsgruppe" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> DeleteWorkgroupMember(int id, CancellationToken ct)
        {
            var success = await _activityService.DeleteWorkgroupMemberAsync(id, ct);
            return success
                ? this.ToastSuccessJson("Medlemmet er fjernet.")
                : this.ToastErrorJson("Medlemmet blev ikke fundet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> SaveTask(SaveTaskRequestDto dto, CancellationToken ct)
        {
            var result = await _activityService.SaveTaskAsync(dto, ct);
            if (!result.Success)
            {
                this.ToastError(result.ErrorMessage ?? "Opgaven kunne ikke gemmes.");
            }
            else
            {
                this.ToastSuccess(dto.Id > 0 ? "Opgaven er opdateret." : "Opgaven er oprettet.");
            }

            return RedirectToAction(nameof(Details), new { id = dto.ActivityId, tab = "opgaver" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> DeleteTask(int id, CancellationToken ct)
        {
            var success = await _activityService.DeleteTaskAsync(id, ct);
            return success
                ? this.ToastSuccessJson("Opgaven er slettet.")
                : this.ToastErrorJson("Opgaven blev ikke fundet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> ToggleTaskCompleted(int id, int activityId, CancellationToken ct)
        {
            var result = await _activityService.ToggleTaskCompletedAsync(id, null, _userManager.GetUserId(User), ct);
            if (!result.Success)
            {
                this.ToastError(result.ErrorMessage ?? "Opgaven kunne ikke opdateres.");
            }

            return RedirectToAction(nameof(Details), new { id = activityId, tab = "opgaver" });
        }

        [HttpGet]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> CheckFormResponses(int formId, CancellationToken ct)
        {
            var result = await _activityService.CheckFormResponsesAsync(formId, ct);
            return Json(new { hasResponses = result.HasResponses, responseCount = result.ResponseCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> LinkForm(int id, int formId, bool createNewVersion, CancellationToken ct)
        {
            var result = await _activityService.LinkFormAsync(id, formId, createNewVersion, _userManager.GetUserId(User), ct);
            if (result.Success)
            {
                this.ToastSuccess("Formular er linket.");
            }
            else
            {
                this.ToastError(result.ErrorMessage ?? "Formularen kunne ikke linkes.");
            }

            return RedirectToAction(nameof(Details), new { id, tab = "svar" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> UnlinkForm(int id, int formId, CancellationToken ct)
        {
            var result = await _activityService.UnlinkFormAsync(id, formId, ct);
            if (result.Success)
            {
                this.ToastSuccess("Formular-link er fjernet.");
            }
            else
            {
                this.ToastError(result.ErrorMessage ?? "Linket kunne ikke fjernes.");
            }

            return RedirectToAction(nameof(Details), new { id, tab = "formular" });
        }

        private async Task<List<PersonGroupOptionViewModel>> GetGroupOptionsAsync(CancellationToken ct)
        {
            var shell = await _activityService.GetFormShellAsync(ct);
            return shell.GroupOptions;
        }
    }
}
