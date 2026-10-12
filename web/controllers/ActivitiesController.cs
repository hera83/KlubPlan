using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Models;
using web.Extensions;
using web.Managers.Activities.Dtos;
using web.Managers.Activities.Interfaces;
using web.Managers.Communication.Interfaces;
using web.Managers.Forms.Interfaces;
using web.Repositories.Activities.Dtos;
using web.Repositories.Activities.Interfaces;
using web.Repositories.ActivityFiles.Interfaces;
using web.Repositories.ActivityLists.Interfaces;
using web.ViewModels;
using web.ViewModels.Mappers;

namespace web.Controllers;

/// <summary>
/// Activities and the activity page. The page's tabs come from their own areas — the activity itself
/// (IActivityManager), form responses (IFormManager), Kommunikation (ICommunicationManager), Filer and
/// Lister — because managers don't call each other.
/// </summary>
[Authorize]
public class ActivitiesController(
    IActivityManager activityManager,
    IActivityRepository activityRepository,
    IFormManager formManager,
    ICommunicationManager communicationManager,
    IActivityFileRepository activityFileRepository,
    IActivityListRepository activityListRepository,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var list = await activityManager.GetListAsync(new SearchActivitiesDto(), cancellationToken);
        return View(list.ToActivityFilterViewModel(null));
    }

    [HttpGet]
    public async Task<IActionResult> ActivitiesTable(ActivityFilterViewModel filter, CancellationToken cancellationToken)
    {
        var list = await activityManager.GetListAsync(filter.ToSearchActivitiesDto(), cancellationToken);
        return PartialView("_ActivitiesTableBody", list.ToActivityFilterViewModel(filter.SearchText));
    }

    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var form = await activityManager.GetFormAsync(null, cancellationToken);
        return View("Form", form!.ToActivityFormViewModel());
    }

    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var form = await activityManager.GetFormAsync(id, cancellationToken);
        if (form is null)
        {
            this.ToastError("Aktiviteten blev ikke fundet.");
            return RedirectToAction(nameof(Index));
        }

        return View("Form", form.ToActivityFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> Save(ActivityFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            this.ToastError("Aktiviteten kunne ikke gemmes. Kontroller felterne.");
            return await RedisplayFormAsync(model, cancellationToken);
        }

        var result = await activityRepository.SaveAsync(model.ToSaveActivityDto(userManager.GetUserId(User)), cancellationToken);
        if (result.Status != SaveActivityStatus.Saved)
        {
            this.ToastError(result.Status switch
            {
                SaveActivityStatus.TitleRequired => "Aktiviteten skal have en titel.",
                SaveActivityStatus.EndNotAfterStart => "Sluttidspunkt skal være efter starttidspunkt.",
                SaveActivityStatus.NotFound => "Aktiviteten blev ikke fundet.",
                _ => "Aktiviteten kunne ikke gemmes."
            });
            return await RedisplayFormAsync(model, cancellationToken);
        }

        this.ToastSuccess(model.Id > 0 ? "Aktiviteten er opdateret." : "Aktiviteten er oprettet.");
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await activityManager.DeleteAsync(id, cancellationToken);
        return success
            ? this.ToastSuccessJson("Aktiviteten er slettet.")
            : this.ToastErrorJson("Aktiviteten blev ikke fundet.");
    }

    public async Task<IActionResult> Details(int id, string? tab, int? folder, CancellationToken cancellationToken)
    {
        var details = await activityManager.GetDetailsAsync(id, cancellationToken);
        if (details is null)
        {
            this.ToastError("Aktiviteten blev ikke fundet.");
            return RedirectToAction(nameof(Index));
        }

        var model = details.ToActivityDetailsViewModel();
        foreach (var linkedForm in model.LinkedForms)
        {
            linkedForm.FormResponses = (await formManager.GetResponsesAsync(linkedForm.FormId, page: 1, pageSize: 10, cancellationToken))?.ToFormResponsesViewModel();
        }

        model.Messages = (await communicationManager.GetMessagesAsync(id, cancellationToken)).ToCommunicationMessageListItemViewModels();
        model.ComposeOptions = (await communicationManager.GetComposeOptionsAsync(cancellationToken)).ToComposeOptionsViewModel();

        if (User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.Developer))
        {
            model.Files = (await activityFileRepository.GetFolderAsync(id, folder, null, cancellationToken))?.ToActivityFilesViewModel();
            model.Lists = (await activityListRepository.GetSummariesAsync(id, userManager.GetUserId(User), cancellationToken)).ToActivityListSummaryViewModels();
        }

        ViewData["ActiveTab"] = string.IsNullOrWhiteSpace(tab) ? "oversigt" : tab;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> SaveWorkgroupMember(ActivityWorkgroupMemberFormViewModel model, CancellationToken cancellationToken)
    {
        var status = await activityRepository.SaveWorkgroupMemberAsync(model.ToSaveActivityWorkgroupMemberDto(), cancellationToken);
        if (status == SaveActivityWorkgroupMemberStatus.Saved)
        {
            this.ToastSuccess(model.Id > 0 ? "Medlemmet er opdateret." : "Medlemmet er tilføjet.");
        }
        else
        {
            this.ToastError(status switch
            {
                SaveActivityWorkgroupMemberStatus.NameRequired => "Vælg en administrator, eller udfyld navn på den eksterne kontakt.",
                SaveActivityWorkgroupMemberStatus.NotFound => "Medlemmet blev ikke fundet.",
                SaveActivityWorkgroupMemberStatus.ActivityNotFound => "Aktiviteten blev ikke fundet.",
                SaveActivityWorkgroupMemberStatus.UserNotFound => "Administratoren blev ikke fundet.",
                SaveActivityWorkgroupMemberStatus.AlreadyMember => "Denne administrator er allerede en del af arbejdsgruppen.",
                _ => "Medlemmet kunne ikke gemmes."
            });
        }

        return RedirectToAction(nameof(Details), new { id = model.ActivityId, tab = "arbejdsgruppe" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> DeleteWorkgroupMember(int id, CancellationToken cancellationToken)
    {
        var success = await activityRepository.DeleteWorkgroupMemberAsync(id, cancellationToken);
        return success
            ? this.ToastSuccessJson("Medlemmet er fjernet.")
            : this.ToastErrorJson("Medlemmet blev ikke fundet.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> SaveTask(ActivityTaskFormViewModel model, CancellationToken cancellationToken)
    {
        var status = await activityRepository.SaveTaskAsync(model.ToSaveActivityTaskDto(), cancellationToken);
        if (status == SaveActivityTaskStatus.Saved)
        {
            this.ToastSuccess(model.Id > 0 ? "Opgaven er opdateret." : "Opgaven er oprettet.");
        }
        else
        {
            this.ToastError(status switch
            {
                SaveActivityTaskStatus.TitleRequired => "Opgaven skal have en titel.",
                SaveActivityTaskStatus.NotFound => "Opgaven blev ikke fundet.",
                SaveActivityTaskStatus.ActivityNotFound => "Aktiviteten blev ikke fundet.",
                SaveActivityTaskStatus.AssigneeNotFound => "Den valgte ansvarlige findes ikke på denne aktivitet.",
                _ => "Opgaven kunne ikke gemmes."
            });
        }

        return RedirectToAction(nameof(Details), new { id = model.ActivityId, tab = "opgaver" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> DeleteTask(int id, CancellationToken cancellationToken)
    {
        var success = await activityRepository.DeleteTaskAsync(id, cancellationToken);
        return success
            ? this.ToastSuccessJson("Opgaven er slettet.")
            : this.ToastErrorJson("Opgaven blev ikke fundet.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> ToggleTaskCompleted(int id, int activityId, CancellationToken cancellationToken)
    {
        if (!await activityRepository.ToggleTaskCompletedAsync(id, userManager.GetUserId(User), cancellationToken))
        {
            this.ToastError("Opgaven blev ikke fundet.");
        }

        return RedirectToAction(nameof(Details), new { id = activityId, tab = "opgaver" });
    }

    [HttpGet]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> CheckFormResponses(int formId, CancellationToken cancellationToken)
    {
        var responses = await formManager.GetResponsesAsync(formId, page: 1, pageSize: 1, cancellationToken);
        var count = responses?.TotalCount ?? 0;
        return Json(new { hasResponses = count > 0, responseCount = count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> LinkForm(int id, int formId, bool createNewVersion, CancellationToken cancellationToken)
    {
        var status = await activityManager.LinkFormAsync(id, formId, createNewVersion, userManager.GetUserId(User), cancellationToken);
        if (status == LinkActivityFormStatus.Linked)
        {
            this.ToastSuccess("Formular er linket.");
        }
        else
        {
            this.ToastError(status switch
            {
                LinkActivityFormStatus.ActivityNotFound => "Aktiviteten blev ikke fundet.",
                LinkActivityFormStatus.FormNotFound => "Formularen blev ikke fundet.",
                LinkActivityFormStatus.StillAcceptingResponses => "Der kan kun oprettes en ny version, når formularen er lukket for svar.",
                LinkActivityFormStatus.NotLatestVersion => "Der findes allerede en nyere version af denne formular.",
                _ => "Formularen kunne ikke linkes."
            });
        }

        return RedirectToAction(nameof(Details), new { id, tab = "svar" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> UnlinkForm(int id, int formId, CancellationToken cancellationToken)
    {
        if (await activityRepository.UnlinkFormAsync(id, formId, cancellationToken))
        {
            this.ToastSuccess("Formular-link er fjernet.");
        }
        else
        {
            this.ToastError("Formularen er ikke linket til denne aktivitet.");
        }

        return RedirectToAction(nameof(Details), new { id, tab = "formular" });
    }

    private async Task<IActionResult> RedisplayFormAsync(ActivityFormViewModel model, CancellationToken cancellationToken)
    {
        var form = await activityManager.GetFormAsync(null, cancellationToken);
        model.GroupOptions = form!.Groups.ToPersonGroupOptionViewModels();
        return View("Form", model);
    }
}
