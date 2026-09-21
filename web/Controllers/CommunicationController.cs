using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.Communication.Dtos;
using web.Repositories.Communication.Interfaces;

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
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var model = await _communicationService.GetDetailsAsync(id, ct);
            if (model is null)
            {
                this.ToastError("Beskeden blev ikke fundet.");
                return RedirectToAction(nameof(Index));
            }

            return View(model);
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
                return RedirectToAction(nameof(Index));
            }

            this.ToastSuccess(dto.Action == "send" ? "Beskeden er sendt." : "Beskeden er gemt som kladde.");
            return RedirectToAction(nameof(Index));
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
