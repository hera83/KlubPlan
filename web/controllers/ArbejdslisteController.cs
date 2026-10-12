using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Extensions;
using web.Repositories.ActivityLists.Interfaces;
using web.ViewModels;
using web.ViewModels.Mappers;

namespace web.Controllers;

/// <summary>
/// Public, unauthenticated work list links: /Arbejdsliste?Id={list.Id}&amp;UId={member.PublicId}.
/// Lets an external contact in an activity's arbejdsgruppe (ActivityWorkgroupMember without a login)
/// work on the lines of a list that are assigned to them — and only those. UId is the unguessable
/// ActivityWorkgroupMember.PublicId; the list must belong to the same activity as the member.
/// Mirrors FormularController's public /Formular link.
/// </summary>
[AllowAnonymous]
public class ArbejdslisteController(IActivityListRepository activityListRepository) : Controller
{
    public async Task<IActionResult> Index(int id, Guid uId, CancellationToken cancellationToken)
    {
        var list = await activityListRepository.GetPublicListAsync(id, uId, cancellationToken);
        return View(list.ToPublicWorkListViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> ItemsTable(Guid uId, ActivityListItemFilterViewModel filter, CancellationToken cancellationToken)
    {
        var page = await activityListRepository.GetPublicItemsAsync(uId, filter.ToSearchActivityListItemsDto(), cancellationToken);
        return page is null ? NotFound() : PartialView("_ItemsTableBody", page.ToPublicWorkListItemsViewModel());
    }

    /// <summary>Inline autosave of status, note or an extra column on one of the member's own lines.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateField(Guid uId, ActivityListFieldUpdateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson("Feltet kunne ikke gemmes.");

        var result = await activityListRepository.UpdatePublicFieldAsync(uId, model.ToUpdateActivityListFieldDto(), cancellationToken);
        if (!result.Success)
            return this.ToastErrorJson(result.ErrorMessage ?? "Feltet kunne ikke gemmes.");

        return Json(new { success = true, statusCounts = result.StatusCounts });
    }
}
