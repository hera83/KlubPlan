using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Infrastructure;
using web.Repositories.ActivityLists.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    /// <summary>
    /// Public, unauthenticated work list links: /Arbejdsliste?Id={list.Id}&amp;UId={member.PublicId}.
    /// Lets an external contact in an activity's arbejdsgruppe (ActivityWorkgroupMember without a login)
    /// work on the lines of a list that are assigned to them — and only those. UId is the unguessable
    /// ActivityWorkgroupMember.PublicId; the list must belong to the same activity as the member.
    /// Mirrors FormularController's public /Formular link.
    /// </summary>
    [AllowAnonymous]
    public class ArbejdslisteController : Controller
    {
        private readonly IActivityListService _listService;

        public ArbejdslisteController(IActivityListService listService)
        {
            _listService = listService;
        }

        public async Task<IActionResult> Index(int id, Guid uId, CancellationToken ct)
        {
            var model = await _listService.GetPublicListAsync(id, uId, ct);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ItemsTable(Guid uId, ActivityListItemFilterViewModel filter, CancellationToken ct)
        {
            var model = await _listService.GetPublicItemsAsync(uId, filter, ct);
            return model is null ? NotFound() : PartialView("_ItemsTableBody", model);
        }

        /// <summary>Inline autosave of status, note or an extra column on one of the member's own lines.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateField(Guid uId, ActivityListFieldUpdateViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Feltet kunne ikke gemmes.");

            var result = await _listService.UpdatePublicFieldAsync(uId, model, ct);
            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Feltet kunne ikke gemmes.");

            return Json(new { success = true, statusCounts = result.StatusCounts });
        }
    }
}
