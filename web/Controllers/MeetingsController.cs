using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.Meetings.Dtos;
using web.Repositories.Meetings.Interfaces;
using web.Services.AiGateway;
using web.Services.AiGateway.Dtos.Ollama;
using web.Services.AiGateway.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    [Authorize]
    public class MeetingsController : Controller
    {
        private readonly IMeetingsService _meetingsService;
        private readonly IAiGatewayService _aiGatewayService;
        private readonly IAiGatewayConfigurationProvider _aiGatewayConfigurationProvider;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<MeetingsController> _logger;

        public MeetingsController(
            IMeetingsService meetingsService,
            IAiGatewayService aiGatewayService,
            IAiGatewayConfigurationProvider aiGatewayConfigurationProvider,
            UserManager<ApplicationUser> userManager,
            ILogger<MeetingsController> logger)
        {
            _meetingsService = meetingsService;
            _aiGatewayService = aiGatewayService;
            _aiGatewayConfigurationProvider = aiGatewayConfigurationProvider;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _meetingsService.GetMeetingsAsync(new MeetingFilterViewModel(), HttpContext.RequestAborted);
            ViewData["Admins"] = await _meetingsService.GetAdminOptionsAsync(HttpContext.RequestAborted);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> MeetingsTable(MeetingFilterViewModel filter)
        {
            var model = await _meetingsService.GetMeetingsAsync(filter, HttpContext.RequestAborted);
            return PartialView("_MeetingsTableBody", model);
        }

        [HttpGet]
        public async Task<IActionResult> MeetingDetailsJson(int id)
        {
            var detail = await _meetingsService.GetMeetingDetailAsync(id, HttpContext.RequestAborted);
            if (detail is null)
                return NotFound();

            return Json(detail);
        }

        public async Task<IActionResult> Details(int id)
        {
            var detail = await _meetingsService.GetMeetingDetailAsync(id, HttpContext.RequestAborted);
            if (detail is null)
                return NotFound();

            ViewData["Admins"] = await _meetingsService.GetAdminOptionsAsync(HttpContext.RequestAborted);
            ViewData["Groups"] = await _meetingsService.GetGroupOptionsAsync(HttpContext.RequestAborted);
            return View(detail);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMeeting(CreateMeetingViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Mødet kunne ikke oprettes. Kontroller felterne.");

            var result = await _meetingsService.CreateMeetingAsync(new CreateMeetingRequestDto
            {
                Title = model.Title,
                MeetingDateUtc = model.MeetingDate,
                Location = model.Location,
                GroupIds = model.GroupIds,
                AttendeeUserIds = model.AttendeeUserIds,
                AgendaNotes = model.AgendaNotes
            }, HttpContext.RequestAborted);

            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Mødet kunne ikke oprettes.");

            return Json(new { success = true, message = "Møde oprettet.", type = "success", meetingId = result.MeetingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMeeting(EditMeetingViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Mødet kunne ikke gemmes. Kontroller felterne.");

            var result = await _meetingsService.UpdateMeetingAsync(new UpdateMeetingRequestDto
            {
                Id = model.Id,
                Title = model.Title,
                MeetingDateUtc = model.MeetingDate,
                Location = model.Location,
                GroupIds = model.GroupIds,
                AttendeeUserIds = model.AttendeeUserIds,
                AgendaNotes = model.AgendaNotes,
                Status = model.Status
            }, HttpContext.RequestAborted);

            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Mødet kunne ikke gemmes.");

            return this.ToastSuccessJson("Møde opdateret.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeeting(int id)
        {
            var deleted = await _meetingsService.DeleteMeetingAsync(id, HttpContext.RequestAborted);
            return deleted
                ? this.ToastSuccessJson("Møde slettet.")
                : this.ToastErrorJson("Mødet blev ikke fundet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAttendance(SetMeetingAttendanceViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false });

            var result = await _meetingsService.SetAttendanceAsync(model.MeetingId, model.UserId, model.HasAttended, HttpContext.RequestAborted);
            return Json(new { success = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNotes(SaveMeetingNotesViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false });

            var result = await _meetingsService.SaveNotesAsync(model.MeetingId, model.AgendaNotes, model.MinutesNotes, HttpContext.RequestAborted);
            return Json(new { success = result, savedAtUtc = DateTime.UtcNow });
        }

        [HttpGet]
        public async Task<IActionResult> DecisionsPartial(int meetingId)
        {
            var detail = await _meetingsService.GetMeetingDetailAsync(meetingId, HttpContext.RequestAborted);
            if (detail is null)
                return NotFound();

            ViewData["Admins"] = await _meetingsService.GetAdminOptionsAsync(HttpContext.RequestAborted);
            return PartialView("_MeetingDecisionsList", detail.Decisions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDecision(CreateMeetingDecisionViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Angiv en beskrivelse af beslutningen.");

            var decision = await _meetingsService.AddDecisionAsync(model.MeetingId, model.Description, model.ResponsibleUserId, model.DueDate, HttpContext.RequestAborted);
            return decision is not null
                ? this.ToastSuccessJson("Beslutning tilføjet.")
                : this.ToastErrorJson("Mødet blev ikke fundet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDecision(EditMeetingDecisionViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Angiv en beskrivelse af beslutningen.");

            var result = await _meetingsService.UpdateDecisionAsync(model.Id, model.Description, model.ResponsibleUserId, model.DueDate, model.IsCompleted, HttpContext.RequestAborted);
            return result
                ? this.ToastSuccessJson("Beslutning opdateret.")
                : this.ToastErrorJson("Beslutningen blev ikke fundet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDecision(int id, bool isCompleted)
        {
            var result = await _meetingsService.SetDecisionCompletionAsync(id, isCompleted, HttpContext.RequestAborted);
            return Json(new { success = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDecision(int id)
        {
            var result = await _meetingsService.DeleteDecisionAsync(id, HttpContext.RequestAborted);
            return result
                ? this.ToastSuccessJson("Beslutning slettet.")
                : this.ToastErrorJson("Beslutningen blev ikke fundet.");
        }

        [HttpGet]
        public async Task<IActionResult> AttachmentsPartial(int meetingId)
        {
            var detail = await _meetingsService.GetMeetingDetailAsync(meetingId, HttpContext.RequestAborted);
            if (detail is null)
                return NotFound();

            return PartialView("_MeetingAttachmentsList", detail.Attachments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(UploadMeetingAttachmentViewModel model)
        {
            if (!ModelState.IsValid || model.File is null || model.File.Length == 0)
                return this.ToastErrorJson("Vælg en fil.");

            var userId = _userManager.GetUserId(User);
            await using var stream = model.File.OpenReadStream();
            var attachment = await _meetingsService.AddAttachmentAsync(
                model.MeetingId, stream, model.File.FileName, model.File.ContentType, userId, isRecording: false, HttpContext.RequestAborted);

            return attachment is not null
                ? this.ToastSuccessJson("Fil uploadet.")
                : this.ToastErrorJson("Mødet blev ikke fundet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            var result = await _meetingsService.DeleteAttachmentAsync(id, HttpContext.RequestAborted);
            return result
                ? this.ToastSuccessJson("Fil slettet.")
                : this.ToastErrorJson("Filen blev ikke fundet.");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var file = await _meetingsService.GetAttachmentFileAsync(id, HttpContext.RequestAborted);
            if (file is null)
                return NotFound();

            return File(file.Value.Data, file.Value.ContentType, file.Value.FileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRecording(SaveMeetingRecordingViewModel model)
        {
            if (!ModelState.IsValid || model.Audio is null || model.Audio.Length == 0)
                return this.ToastErrorJson("Ingen lydoptagelse modtaget.");

            var userId = _userManager.GetUserId(User);
            await using var stream = model.Audio.OpenReadStream();
            var attachment = await _meetingsService.AddAttachmentAsync(
                model.MeetingId, stream, model.Audio.FileName ?? "optagelse.webm", model.Audio.ContentType, userId, isRecording: true, HttpContext.RequestAborted);

            return attachment is not null
                ? this.ToastSuccessJson("Optagelse gemt.")
                : this.ToastErrorJson("Mødet blev ikke fundet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TranscribeAttachment(int attachmentId)
        {
            var (success, errorMessage) = await _meetingsService.RequestTranscriptionAsync(attachmentId, HttpContext.RequestAborted);
            return success
                ? this.ToastSuccessJson("Transskription startet i baggrunden.")
                : this.ToastErrorJson(errorMessage ?? "Kunne ikke starte transskription.");
        }

        [HttpGet]
        public async Task<IActionResult> TranscriptionStatusJson(int attachmentId)
        {
            var status = await _meetingsService.GetTranscriptionStatusAsync(attachmentId, HttpContext.RequestAborted);
            return status is null ? NotFound() : Json(status);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateSummary(int meetingId)
        {
            var detail = await _meetingsService.GetMeetingDetailAsync(meetingId, HttpContext.RequestAborted);
            if (detail is null)
                return this.ToastErrorJson("Mødet blev ikke fundet.");

            if (string.IsNullOrWhiteSpace(detail.AgendaNotes) && string.IsNullOrWhiteSpace(detail.MinutesNotes))
                return this.ToastErrorJson("Der er ingen noter at opsummere endnu.");

            var config = await _aiGatewayConfigurationProvider.GetActiveConfigurationAsync(HttpContext.RequestAborted);
            var modelName = config.DefaultChatModel;
            if (string.IsNullOrWhiteSpace(modelName))
                return this.ToastErrorJson("Ingen AI-model er konfigureret.");

            var prompt = $"""
                Du hjælper med at strukturere mødereferater for en dansk forening. Ud fra dagsorden og de
                rå noter nedenfor, skriv et pænt struktureret referat på dansk med korte afsnit/punkter,
                og afslut med en liste over konkrete beslutninger/opgaver, hvis nogen fremgår af noterne.
                Opfind ikke indhold der ikke fremgår af noterne.

                Titel: {detail.Title}

                Dagsorden:
                {(string.IsNullOrWhiteSpace(detail.AgendaNotes) ? "(ingen)" : detail.AgendaNotes)}

                Rå noter:
                {(string.IsNullOrWhiteSpace(detail.MinutesNotes) ? "(ingen)" : detail.MinutesNotes)}
                """;

            try
            {
                var response = await _aiGatewayService.OllamaChatAsync(new ChatRequestDto
                {
                    Model = modelName,
                    Messages = new List<OllamaMessageDto>
                    {
                        new() { Role = "user", Content = prompt }
                    }
                }, HttpContext.RequestAborted);

                var summary = response.Message?.Content?.Trim();
                if (string.IsNullOrWhiteSpace(summary))
                    return this.ToastErrorJson("AI-gatewayen returnerede intet referat.");

                return Json(new { success = true, summary });
            }
            catch (AiGatewayException ex)
            {
                _logger.LogWarning(ex, "Kunne ikke generere mødereferat via AiGateway ({StatusCode}): {Message}", ex.StatusCode, ex.Message);
                return this.ToastErrorJson($"Kunne ikke generere referat: {ex.Message}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke kontakte AiGateway for referat-generering");
                return this.ToastErrorJson("Kunne ikke kontakte AiGateway.");
            }
        }
    }
}
