using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.Communication.Dtos;
using web.Repositories.Communication.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    [Authorize]
    public class CommunicationController : Controller
    {
        private readonly ICommunicationService _communicationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommunicationController(ICommunicationService communicationService, UserManager<ApplicationUser> userManager)
        {
            _communicationService = communicationService;
            _userManager = userManager;
        }

        private bool IsAdmin => User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.Developer);

        private string BaseUrl => $"{Request.Scheme}://{Request.Host}";

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var model = await _communicationService.GetIndexDataAsync(IsAdmin, ct);
            return View(model);
        }

        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Details(int id, string? returnUrl, string? returnLabel, CancellationToken ct)
        {
            var model = await _communicationService.GetDetailsAsync(id, ct);
            if (model is null)
            {
                this.ToastError("Beskeden blev ikke fundet.");
                return RedirectToAction(nameof(Index));
            }

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
        public async Task<IActionResult> RecipientsTable(int id, CommunicationRecipientFilterViewModel filter, CancellationToken ct)
        {
            var model = await _communicationService.GetRecipientsAsync(id, filter, ct);
            if (model is null) return NotFound();

            return PartialView("_RecipientsTableBody", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Save(ComposeMessageRequestDto dto, CancellationToken ct)
        {
            var userId = _userManager.GetUserId(User);
            var result = await _communicationService.SaveMessageAsync(dto, userId, BaseUrl, ct);

            if (!result.Success)
            {
                this.ToastError(result.ErrorMessage ?? "Beskeden kunne ikke gemmes.");
                return dto.ActivityId.HasValue
                    ? RedirectToAction("Details", "Activities", new { id = dto.ActivityId, tab = "kommunikation" })
                    : RedirectToAction(nameof(Index));
            }

            this.ToastSuccess(dto.Action == "send" ? "Beskeden er sendt." : "Beskeden er gemt som kladde.");
            return dto.ActivityId.HasValue
                ? RedirectToAction("Details", "Activities", new { id = dto.ActivityId, tab = "kommunikation" })
                : RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "AdminOrDeveloper")]
        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int id, CancellationToken ct)
        {
            var file = await _communicationService.GetAttachmentFileAsync(id, ct);
            if (file is null) return NotFound();

            return File(file.Value.Data, file.Value.ContentType, file.Value.FileName);
        }

        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> GetResendRecipients(int id, CancellationToken ct)
        {
            var recipients = await _communicationService.GetResendTargetsAsync(id, ct);
            return PartialView("_ResendRecipientsTable", recipients);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> SendExisting(int id, List<int>? personIds, CancellationToken ct)
        {
            var result = await _communicationService.SendExistingAsync(id, BaseUrl, personIds, ct);
            return result.Success
                ? this.ToastSuccessJson("Beskeden er sendt.")
                : this.ToastErrorJson(result.ErrorMessage ?? "Beskeden kunne ikke sendes.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var success = await _communicationService.DeleteMessageAsync(id, ct);
            return success
                ? this.ToastSuccessJson("Beskeden er slettet.")
                : this.ToastErrorJson("Beskeden blev ikke fundet.");
        }
    }
}
