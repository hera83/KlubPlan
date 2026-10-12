using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Models;
using web.Extensions;
using web.Managers.Communication.Dtos;
using web.Managers.Communication.Interfaces;
using web.ViewModels;
using web.ViewModels.Mappers;

namespace web.Controllers;

[Authorize]
public class CommunicationController(ICommunicationManager communicationManager, UserManager<ApplicationUser> userManager) : Controller
{
    private bool IsAdmin => User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.Developer);

    private string BaseUrl => $"{Request.Scheme}://{Request.Host}";

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var options = await communicationManager.GetComposeOptionsAsync(cancellationToken);
        var messages = await communicationManager.GetMessagesAsync(null, cancellationToken);
        return View(options.ToCommunicationIndexViewModel(messages, IsAdmin));
    }

    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> Details(int id, string? returnUrl, string? returnLabel, CancellationToken cancellationToken)
    {
        var details = await communicationManager.GetDetailsAsync(id, cancellationToken);
        if (details is null)
        {
            this.ToastError("Beskeden blev ikke fundet.");
            return RedirectToAction(nameof(Index));
        }

        var model = details.ToCommunicationMessageDetailViewModel();

        // The back-link goes to wherever the user actually opened this message from — the
        // caller passes its own URL as returnUrl (e.g. the Kommunikation tab on an activity,
        // or the Kommunikation index). Falls back to the message's own ActivityId (still
        // correct when a message is opened directly, e.g. from a bookmark), then to the index.
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            model.ReturnUrl = returnUrl;
            model.ReturnLabel = string.IsNullOrWhiteSpace(returnLabel) ? "Tilbage" : returnLabel;
        }
        else if (model.ActivityId.HasValue)
        {
            model.ReturnUrl = Url.Action("Details", "Activities", new { id = model.ActivityId, tab = "kommunikation" }) ?? Url.Action(nameof(Index))!;
            model.ReturnLabel = "Tilbage til aktiviteten";
        }
        else
        {
            model.ReturnUrl = Url.Action(nameof(Index))!;
            model.ReturnLabel = "Tilbage til kommunikation";
        }

        return View(model);
    }

    [Authorize(Policy = "AdminOrDeveloper")]
    [HttpGet]
    public async Task<IActionResult> RecipientsTable(int id, CommunicationRecipientFilterViewModel filter, CancellationToken cancellationToken)
    {
        var search = filter.ToSearchCommunicationRecipientsDto();
        var page = await communicationManager.GetRecipientsAsync(id, search, cancellationToken);
        if (page is null) return NotFound();

        return PartialView("_RecipientsTableBody", page.ToCommunicationRecipientFilterViewModel(search));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> Save(ComposeMessageViewModel model, CancellationToken cancellationToken)
    {
        var result = await communicationManager.SaveAsync(model.ToSaveCommunicationDto(userManager.GetUserId(User)), BaseUrl, cancellationToken);

        if (result.Success)
        {
            this.ToastSuccess(result.Status == SendCommunicationStatus.Sent ? "Beskeden er sendt." : "Beskeden er gemt som kladde.");
        }
        else
        {
            this.ToastError(ErrorMessage(result.Status));
        }

        return model.ActivityId.HasValue
            ? RedirectToAction("Details", "Activities", new { id = model.ActivityId, tab = "kommunikation" })
            : RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "AdminOrDeveloper")]
    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(int id, CancellationToken cancellationToken)
    {
        var file = await communicationManager.GetAttachmentFileAsync(id, cancellationToken);
        if (file is null) return NotFound();

        return PhysicalFile(file.FullPath, file.ContentType, file.OriginalFileName);
    }

    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> GetResendRecipients(int id, CancellationToken cancellationToken)
    {
        var targets = await communicationManager.GetResendTargetsAsync(id, cancellationToken);
        return PartialView("_ResendRecipientsTable", targets.ToCommunicationResendTargetViewModels());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> SendExisting(int id, List<int>? personIds, CancellationToken cancellationToken)
    {
        var result = await communicationManager.SendExistingAsync(id, BaseUrl, personIds, cancellationToken);
        return result.Success
            ? this.ToastSuccessJson("Beskeden er sendt.")
            : this.ToastErrorJson(ErrorMessage(result.Status));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await communicationManager.DeleteAsync(id, cancellationToken);
        return success
            ? this.ToastSuccessJson("Beskeden er slettet.")
            : this.ToastErrorJson("Beskeden blev ikke fundet.");
    }

    private static string ErrorMessage(SendCommunicationStatus status) => status switch
    {
        SendCommunicationStatus.SubjectRequired => "Emne skal udfyldes.",
        SendCommunicationStatus.ChannelRequired => "Vælg mindst én kanal (e-mail eller SMS).",
        SendCommunicationStatus.RecipientRequired => "Vælg mindst én modtager (gruppe eller person).",
        SendCommunicationStatus.NotFound => "Beskeden blev ikke fundet.",
        SendCommunicationStatus.NotDraft => "Kun kladder kan redigeres.",
        SendCommunicationStatus.EmptyBody => "Beskeden kan ikke sendes uden indhold.",
        SendCommunicationStatus.NoRecipients => "Ingen modtagere med kontaktoplysninger for de valgte kanaler blev fundet.",
        _ => "Beskeden kunne ikke gemmes."
    };
}
