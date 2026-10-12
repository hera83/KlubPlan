using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Models;
using web.Extensions;
using web.Infrastructure.Labels;
using web.Managers.ActivityLists.Interfaces;
using web.Repositories.ActivityLists.Interfaces;
using web.ViewModels;
using web.ViewModels.Mappers;

namespace web.Controllers;

/// <summary>
/// Work lists on an activity ("Lister"-fanen). The tab itself lives on Activities/Details; a list
/// opens on its own page here (ActivityLists/Details) because the tables get wide.
/// </summary>
[Authorize(Policy = "AdminOrDeveloper")]
public class ActivityListsController(
    IActivityListRepository activityListRepository,
    IActivityListManager activityListManager,
    UserManager<ApplicationUser> userManager,
    ILogger<ActivityListsController> logger) : Controller
{
    private const string ItemsView = "_ItemsTableBody";

    private string? UserId => userManager.GetUserId(User);

    private IActionResult BackToTab(int activityId)
        => RedirectToAction("Details", "Activities", new { id = activityId, tab = "lister" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(ActivityListRules.MaxImportBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = ActivityListRules.MaxImportBytes + 1024 * 1024)]
    public async Task<IActionResult> Create(ActivityListCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.File is null)
        {
            this.ToastError(FirstModelError() ?? "Listen kunne ikke oprettes.");
            return BackToTab(model.ActivityId);
        }
        if (model.File.Length > ActivityListRules.MaxImportBytes)
        {
            this.ToastError($"Excel-filen er større end {ByteSizeHelper.FormatDanish(ActivityListRules.MaxImportBytes)}.");
            return BackToTab(model.ActivityId);
        }

        // ClosedXML needs a seekable stream.
        await using var buffer = new MemoryStream();
        await model.File.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var result = await activityListManager.CreateFromExcelAsync(model.ToCreateActivityListFromExcelDto(buffer, UserId), cancellationToken);
        if (!result.Success)
        {
            this.ToastError(result.ErrorMessage ?? "Listen kunne ikke oprettes.");
            return BackToTab(model.ActivityId);
        }

        this.ToastSuccess(result.Message ?? "Listen er oprettet.");
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var list = await activityListRepository.GetDetailsAsync(id, UserId, cancellationToken);
        if (list is null)
        {
            this.ToastError("Listen blev ikke fundet.");
            return RedirectToAction("Index", "Activities");
        }

        return View(list.ToActivityListDetailsViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> ItemsTable(ActivityListItemFilterViewModel filter, CancellationToken cancellationToken)
    {
        var page = await activityListRepository.GetItemsAsync(filter.ToSearchActivityListItemsDto(), UserId, cancellationToken);
        return page is null ? NotFound() : PartialView(ItemsView, page.ToActivityListItemsViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> Counts(int listId, CancellationToken cancellationToken)
    {
        var counts = await activityListRepository.GetCountsAsync(listId, cancellationToken);
        return counts is null ? NotFound() : Json(counts);
    }

    [HttpGet]
    public async Task<IActionResult> Export(ActivityListItemFilterViewModel filter, CancellationToken cancellationToken)
    {
        var export = await activityListManager.ExportAsync(filter.ToSearchActivityListItemsDto(), UserId, cancellationToken);
        if (export is null)
            return NotFound();

        return File(export.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", export.FileName);
    }

    /// <summary>"Optælling": the sum of one column over the lines in the table's current search/filter, shown in the card under the table.</summary>
    [HttpGet]
    public async Task<IActionResult> ColumnSum(ActivityListItemFilterViewModel filter, int columnId, CancellationToken cancellationToken)
    {
        var result = await activityListRepository.SumColumnAsync(filter.ToSearchActivityListItemsDto(), columnId, UserId, cancellationToken);
        if (result is null)
            return this.ToastErrorJson("Kolonnen blev ikke fundet.");
        if (!result.Success)
            return this.ToastErrorJson(result.ErrorMessage!);

        return Json(new { success = true, result.ColumnId, result.ColumnName, result.Sum, result.SumText, result.Counted, result.Skipped, result.Filtered });
    }

    /// <summary>Saves (or with no columnId removes) the list's "Optælling" column.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetSumColumn(int listId, int? columnId, CancellationToken cancellationToken)
    {
        var result = await activityListRepository.SetSumColumnAsync(listId, columnId, cancellationToken);
        return result.Success ? this.ToastSuccessJson(result.Message!) : this.ToastErrorJson(result.ErrorMessage!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ActivityListUpdateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson(FirstModelError() ?? "Listen kunne ikke gemmes.");

        var result = await activityListRepository.UpdateAsync(model.ListId, model.Title, model.Description, cancellationToken);
        return result.Success ? this.ToastSuccessJson("Listen er gemt.") : this.ToastErrorJson(result.ErrorMessage!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await activityListRepository.DeleteAsync(id, cancellationToken);
        return result.Success ? this.ToastSuccessJson("Listen er slettet.") : this.ToastErrorJson(result.ErrorMessage!);
    }

    /// <summary>Inline autosave of one field (status, note, tilknyttet or an extra column).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateField(ActivityListFieldUpdateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson("Feltet kunne ikke gemmes.");

        var result = await activityListRepository.UpdateFieldAsync(model.ToUpdateActivityListFieldDto(), UserId, cancellationToken);
        if (!result.Success)
            return this.ToastErrorJson(result.ErrorMessage ?? "Feltet kunne ikke gemmes.");

        return Json(new
        {
            success = true,
            statusCounts = result.StatusCounts,
            unassignedCount = result.UnassignedCount,
            updatedText = ActivityListsMapper.UpdatedText(result.UpdatedByName, result.UpdatedAtUtc)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveItem(ActivityListItemSaveViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson("Linjen kunne ikke gemmes.");

        var result = await activityListRepository.SaveItemAsync(model.ListId, model.ItemId, model.Values, UserId, cancellationToken);
        return result.Success ? this.ToastSuccessJson(result.Message ?? "Linjen er gemt.") : this.ToastErrorJson(result.ErrorMessage!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(int listId, int itemId, CancellationToken cancellationToken)
    {
        var result = await activityListRepository.DeleteItemAsync(listId, itemId, cancellationToken);
        return result.Success ? this.ToastSuccessJson("Linjen er slettet.") : this.ToastErrorJson(result.ErrorMessage!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Distribute(ActivityListDistributeViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson("Linjerne kunne ikke fordeles.");

        var result = await activityListRepository.DistributeAsync(model.ListId, model.MemberIds, model.OnlyUnassigned, UserId, cancellationToken);
        return result.Success ? this.ToastSuccessJson(result.Message ?? "Linjerne er fordelt.") : this.ToastErrorJson(result.ErrorMessage!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveColumn(ActivityListColumnSaveViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson(FirstModelError() ?? "Kolonnen kunne ikke gemmes.");

        var result = await activityListRepository.SaveColumnAsync(model.ToSaveActivityListColumnDto(), cancellationToken);
        return result.Success ? this.ToastSuccessJson(result.Message ?? "Kolonnen er gemt.") : this.ToastErrorJson(result.ErrorMessage!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteColumn(int listId, int columnId, CancellationToken cancellationToken)
    {
        var result = await activityListRepository.DeleteColumnAsync(listId, columnId, cancellationToken);
        return result.Success ? this.ToastSuccessJson(result.Message ?? "Kolonnen er slettet.") : this.ToastErrorJson(result.ErrorMessage!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetColumnHidden(int listId, int columnId, bool hidden, CancellationToken cancellationToken)
    {
        var result = await activityListRepository.SetColumnHiddenAsync(listId, columnId, hidden, cancellationToken);
        return result.Success ? this.ToastSuccessJson(result.Message!) : this.ToastErrorJson(result.ErrorMessage!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetNoteVisible(int listId, bool visible, CancellationToken cancellationToken)
    {
        var result = await activityListRepository.SetNoteVisibleAsync(listId, visible, cancellationToken);
        return result.Success ? this.ToastSuccessJson(result.Message!) : this.ToastErrorJson(result.ErrorMessage!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveStatuses(ActivityListStatusesSaveViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson(FirstModelError() ?? "Statusserne kunne ikke gemmes.");

        var result = await activityListRepository.SaveStatusesAsync(model.ToSaveActivityListStatusesDto(), cancellationToken);
        return result.Success ? this.ToastSuccessJson(result.Message!) : this.ToastErrorJson(result.ErrorMessage!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendLinks(ActivityListSendLinksViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson(FirstModelError() ?? "Links kunne ikke sendes.");

        var result = await activityListManager.SendLinksAsync(model.ToSendActivityListLinksDto(), $"{Request.Scheme}://{Request.Host}", cancellationToken);
        return result.Success ? this.ToastSuccessJson(result.Message!) : this.ToastErrorJson(result.ErrorMessage!);
    }

    // ─── Labels ─────────────────────────────────────────────────────────────

    /// <summary>The line's labels for the "Labels" modal.</summary>
    [HttpGet]
    public async Task<IActionResult> ItemLabels(int listId, int itemId, CancellationToken cancellationToken)
    {
        var labels = await activityListRepository.GetItemLabelsAsync(listId, itemId, cancellationToken);
        return labels is null
            ? this.ToastErrorJson("Linjen blev ikke fundet.")
            : Json(new { success = true, labels.RowNumber, Labels = labels.ToLabelInputViewModels() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveItemLabels(ActivityListItemLabelsSaveViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson(FirstModelError() ?? "Labels kunne ikke gemmes.");

        var result = await activityListRepository.SaveItemLabelsAsync(model.ListId, model.ItemId, model.ToActivityListLabelDtos(), cancellationToken);
        return result.Success ? this.ToastSuccessJson(result.Message ?? "Labels er gemt.") : this.ToastErrorJson(result.ErrorMessage!);
    }

    /// <summary>How many labels "Print labels" would print for the chosen scope (whole list or current filter).</summary>
    [HttpGet]
    public async Task<IActionResult> LabelsCount(ActivityListItemFilterViewModel filter, CancellationToken cancellationToken)
    {
        var labels = await activityListRepository.GetPrintLabelsAsync(filter.ToSearchActivityListItemsDto(), UserId, cancellationToken);
        return labels is null ? NotFound() : Json(new { lines = labels.Lines, copies = labels.Copies });
    }

    /// <summary>
    /// The label sheets as a PDF, shown in the browser (new tab) so it can be printed from there.
    /// filter and sheet bind from the same query; the filter fields are only set for "Kun linjer i nuværende filter".
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> LabelsPdf(ActivityListItemFilterViewModel filter, LabelSheetViewModel sheet, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            this.ToastError(FirstModelError() ?? "Labels kunne ikke printes.");
            return RedirectToAction(nameof(Details), new { id = filter.ListId });
        }

        var labels = await activityListRepository.GetPrintLabelsAsync(filter.ToSearchActivityListItemsDto(), UserId, cancellationToken);
        if (labels is null)
            return NotFound();

        var pdf = LabelSheetPdf.Create(labels.Title, labels.ToLabelRows(), sheet, logger,
            emptyMessage: filter.HasFilter
                ? "Der er ingen labels på linjerne i det nuværende filter."
                : "Der er ingen labels at printe. Tilføj labels på linjerne først.",
            tooManyHint: "Brug filteret og print dem i flere omgange.");
        if (!pdf.Success)
        {
            this.ToastWarning(pdf.ErrorMessage!);
            return RedirectToAction(nameof(Details), new { id = filter.ListId });
        }

        return this.InlineLabelPdf(pdf);
    }

    private string? FirstModelError()
        => ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
}
