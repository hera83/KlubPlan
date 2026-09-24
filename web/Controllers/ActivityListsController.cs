using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.ActivityLists.Dtos;
using web.Repositories.ActivityLists.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    /// <summary>
    /// Work lists on an activity ("Lister"-fanen). The tab itself lives on Activities/Details; a list
    /// opens on its own page here (ActivityLists/Details) because the tables get wide.
    /// </summary>
    [Authorize(Policy = "AdminOrDeveloper")]
    public class ActivityListsController : Controller
    {
        private const string ItemsView = "_ItemsTableBody";

        private readonly IActivityListService _listService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivityListsController(IActivityListService listService, UserManager<ApplicationUser> userManager)
        {
            _listService = listService;
            _userManager = userManager;
        }

        private string? UserId => _userManager.GetUserId(User);

        private IActionResult BackToTab(int activityId)
            => RedirectToAction("Details", "Activities", new { id = activityId, tab = "lister" });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(ActivityListRules.MaxImportBytes + 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = ActivityListRules.MaxImportBytes + 1024 * 1024)]
        public async Task<IActionResult> Create(ActivityListCreateViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid || model.File is null)
            {
                this.ToastError(FirstModelError() ?? "Listen kunne ikke oprettes.");
                return BackToTab(model.ActivityId);
            }
            if (model.File.Length > ActivityListRules.MaxImportBytes)
            {
                this.ToastError($"Excel-filen er større end {ByteSizeFormatter.FormatDanish(ActivityListRules.MaxImportBytes)}.");
                return BackToTab(model.ActivityId);
            }

            // ClosedXML needs a seekable stream.
            await using var buffer = new MemoryStream();
            await model.File.CopyToAsync(buffer, ct);
            buffer.Position = 0;

            var result = await _listService.CreateAsync(new CreateActivityListRequestDto
            {
                ActivityId = model.ActivityId,
                Title = model.Title,
                Description = model.Description,
                FileName = model.File.FileName,
                Content = buffer,
                UserId = UserId
            }, ct);

            if (!result.Success)
            {
                this.ToastError(result.ErrorMessage ?? "Listen kunne ikke oprettes.");
                return BackToTab(model.ActivityId);
            }

            this.ToastSuccess(result.Message ?? "Listen er oprettet.");
            return RedirectToAction(nameof(Details), new { id = result.Id });
        }

        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var model = await _listService.GetDetailsAsync(id, UserId, ct);
            if (model is null)
            {
                this.ToastError("Listen blev ikke fundet.");
                return RedirectToAction("Index", "Activities");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ItemsTable(ActivityListItemFilterViewModel filter, CancellationToken ct)
        {
            var model = await _listService.GetItemsAsync(filter, UserId, ct);
            return model is null ? NotFound() : PartialView(ItemsView, model);
        }

        [HttpGet]
        public async Task<IActionResult> Counts(int listId, CancellationToken ct)
        {
            var counts = await _listService.GetCountsAsync(listId, ct);
            return counts is null ? NotFound() : Json(counts);
        }

        [HttpGet]
        public async Task<IActionResult> Export(ActivityListItemFilterViewModel filter, CancellationToken ct)
        {
            var export = await _listService.ExportAsync(filter, UserId, ct);
            if (export is null)
                return NotFound();

            return File(export.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", export.FileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ActivityListUpdateViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson(FirstModelError() ?? "Listen kunne ikke gemmes.");

            var result = await _listService.UpdateAsync(model.ListId, model.Title, model.Description, ct);
            return result.Success ? this.ToastSuccessJson("Listen er gemt.") : this.ToastErrorJson(result.ErrorMessage!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _listService.DeleteAsync(id, ct);
            return result.Success ? this.ToastSuccessJson("Listen er slettet.") : this.ToastErrorJson(result.ErrorMessage!);
        }

        /// <summary>Inline autosave of one field (status, note, tilknyttet or an extra column).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateField(ActivityListFieldUpdateViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Feltet kunne ikke gemmes.");

            var result = await _listService.UpdateFieldAsync(model, UserId, ct);
            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Feltet kunne ikke gemmes.");

            return Json(new
            {
                success = true,
                statusCounts = result.StatusCounts,
                unassignedCount = result.UnassignedCount,
                updatedText = result.UpdatedText
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveItem(ActivityListItemSaveViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Linjen kunne ikke gemmes.");

            var result = await _listService.SaveItemAsync(model.ListId, model.ItemId, model.Values, UserId, ct);
            return result.Success ? this.ToastSuccessJson(result.Message ?? "Linjen er gemt.") : this.ToastErrorJson(result.ErrorMessage!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int listId, int itemId, CancellationToken ct)
        {
            var result = await _listService.DeleteItemAsync(listId, itemId, ct);
            return result.Success ? this.ToastSuccessJson("Linjen er slettet.") : this.ToastErrorJson(result.ErrorMessage!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Distribute(ActivityListDistributeViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Linjerne kunne ikke fordeles.");

            var result = await _listService.DistributeAsync(model.ListId, model.MemberIds, model.OnlyUnassigned, UserId, ct);
            return result.Success ? this.ToastSuccessJson(result.Message ?? "Linjerne er fordelt.") : this.ToastErrorJson(result.ErrorMessage!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveColumn(ActivityListColumnSaveViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson(FirstModelError() ?? "Kolonnen kunne ikke gemmes.");

            var result = await _listService.SaveColumnAsync(model, ct);
            return result.Success ? this.ToastSuccessJson(result.Message ?? "Kolonnen er gemt.") : this.ToastErrorJson(result.ErrorMessage!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteColumn(int listId, int columnId, CancellationToken ct)
        {
            var result = await _listService.DeleteColumnAsync(listId, columnId, ct);
            return result.Success ? this.ToastSuccessJson(result.Message ?? "Kolonnen er slettet.") : this.ToastErrorJson(result.ErrorMessage!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetColumnHidden(int listId, int columnId, bool hidden, CancellationToken ct)
        {
            var result = await _listService.SetColumnHiddenAsync(listId, columnId, hidden, ct);
            return result.Success ? this.ToastSuccessJson(result.Message!) : this.ToastErrorJson(result.ErrorMessage!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetNoteVisible(int listId, bool visible, CancellationToken ct)
        {
            var result = await _listService.SetNoteVisibleAsync(listId, visible, ct);
            return result.Success ? this.ToastSuccessJson(result.Message!) : this.ToastErrorJson(result.ErrorMessage!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStatuses(ActivityListStatusesSaveViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson(FirstModelError() ?? "Statusserne kunne ikke gemmes.");

            var result = await _listService.SaveStatusesAsync(model, ct);
            return result.Success ? this.ToastSuccessJson(result.Message!) : this.ToastErrorJson(result.ErrorMessage!);
        }

        private string? FirstModelError()
            => ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
    }
}
