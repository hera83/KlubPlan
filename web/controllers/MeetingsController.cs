using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Data.Models;
using web.Extensions;
using web.Managers.Meetings.Dtos;
using web.Managers.Meetings.Interfaces;
using web.Repositories.Meetings.Dtos;
using web.Repositories.Meetings.Interfaces;
using web.ViewModels;
using web.ViewModels.Mappers;

namespace web.Controllers;

[Authorize]
public class MeetingsController(
    IMeetingRepository meetingRepository,
    IMeetingManager meetingManager,
    UserManager<ApplicationUser> userManager) : Controller
{
    private const string NotFoundMessage = "Mødet blev ikke fundet.";

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var filter = new MeetingFilterViewModel();
        var overview = await meetingManager.GetOverviewAsync(filter.ToSearchMeetingsDto(), cancellationToken);
        ViewData["Admins"] = overview.Admins.ToMeetingAdminOptionViewModels();
        return View(overview.ToMeetingFilterViewModel(filter));
    }

    [HttpGet]
    public async Task<IActionResult> MeetingsTable(MeetingFilterViewModel filter, CancellationToken cancellationToken)
    {
        var overview = await meetingManager.GetOverviewAsync(filter.ToSearchMeetingsDto(), cancellationToken);
        return PartialView("_MeetingsTableBody", overview.ToMeetingFilterViewModel(filter));
    }

    [HttpGet]
    public async Task<IActionResult> MeetingDetailsJson(int id, CancellationToken cancellationToken)
    {
        var details = await meetingManager.GetDetailsAsync(id, cancellationToken);
        return details is null ? NotFound() : Json(details.Meeting.ToMeetingDetailViewModel(details.Groups.Count));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var details = await meetingManager.GetDetailsAsync(id, cancellationToken);
        if (details is null)
            return NotFound();

        ViewData["Admins"] = details.Admins.ToMeetingAdminOptionViewModels();
        ViewData["Groups"] = details.Groups.ToPersonGroupOptionViewModels();
        return View(details.Meeting.ToMeetingDetailViewModel(details.Groups.Count));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMeeting(CreateMeetingViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson("Mødet kunne ikke oprettes. Kontroller felterne.");

        var meetingId = await meetingRepository.CreateAsync(model.ToCreateMeetingDto(), cancellationToken);
        return Json(new { success = true, message = "Møde oprettet.", type = "success", meetingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMeeting(EditMeetingViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson("Mødet kunne ikke gemmes. Kontroller felterne.");

        return await meetingRepository.UpdateAsync(model.Id, model.ToUpdateMeetingDto(), cancellationToken) switch
        {
            SaveMeetingStatus.Saved => this.ToastSuccessJson("Møde opdateret."),
            SaveMeetingStatus.NotLatestVersion => this.ToastErrorJson("Der findes en nyere version af dette møde — kun den aktuelle version kan redigeres."),
            _ => this.ToastErrorJson(NotFoundMessage)
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateNewVersion(int id, DateTime meetingDate, CancellationToken cancellationToken)
    {
        if (meetingDate == default)
            return this.ToastErrorJson("Angiv dato og tid for den nye version.");

        var result = await meetingRepository.CreateNewVersionAsync(id, meetingDate, cancellationToken);
        return result.Status switch
        {
            CreateMeetingVersionStatus.Created => Json(new { success = true, message = "Ny version oprettet.", type = "success", meetingId = result.MeetingId }),
            CreateMeetingVersionStatus.StillPlanned => this.ToastErrorJson("Der kan kun oprettes en ny version, når mødet er afholdt eller aflyst."),
            CreateMeetingVersionStatus.NotLatestVersion => this.ToastErrorJson("Der findes allerede en nyere version af dette møde."),
            _ => this.ToastErrorJson(NotFoundMessage)
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMeeting(int id, CancellationToken cancellationToken) =>
        await meetingManager.DeleteAsync(id, cancellationToken)
            ? this.ToastSuccessJson("Møde slettet.")
            : this.ToastErrorJson(NotFoundMessage);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAttendance(SetMeetingAttendanceViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false });

        var success = await meetingRepository.SetAttendanceAsync(model.MeetingId, model.UserId, model.HasAttended, cancellationToken);
        return Json(new { success });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveNotes(SaveMeetingNotesViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false });

        var success = await meetingRepository.SaveNotesAsync(model.MeetingId, model.AgendaNotes, model.MinutesNotes, cancellationToken);
        return Json(new { success, savedAtUtc = DateTime.UtcNow });
    }

    [HttpGet]
    public async Task<IActionResult> DecisionsPartial(int meetingId, CancellationToken cancellationToken)
    {
        var details = await meetingManager.GetDetailsAsync(meetingId, cancellationToken);
        if (details is null)
            return NotFound();

        ViewData["Admins"] = details.Admins.ToMeetingAdminOptionViewModels();
        ViewData["IsReadOnly"] = !details.Meeting.VersionInfo.IsLatestVersion;
        return PartialView("_MeetingDecisionsList", details.Meeting.Decisions.Select(d => d.ToMeetingDecisionViewModel()).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDecision(CreateMeetingDecisionViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson("Angiv en beskrivelse af beslutningen.");

        return await meetingRepository.AddDecisionAsync(model.MeetingId, model.ToCreateMeetingDecisionDto(), cancellationToken) is not null
            ? this.ToastSuccessJson("Beslutning tilføjet.")
            : this.ToastErrorJson(NotFoundMessage);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDecision(EditMeetingDecisionViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson("Angiv en beskrivelse af beslutningen.");

        return await meetingRepository.UpdateDecisionAsync(model.Id, model.ToUpdateMeetingDecisionDto(), cancellationToken)
            ? this.ToastSuccessJson("Beslutning opdateret.")
            : this.ToastErrorJson("Beslutningen blev ikke fundet.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDecision(int id, bool isCompleted, CancellationToken cancellationToken)
    {
        var success = await meetingRepository.SetDecisionCompletionAsync(id, isCompleted, cancellationToken);
        return Json(new { success });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDecision(int id, CancellationToken cancellationToken) =>
        await meetingRepository.DeleteDecisionAsync(id, cancellationToken)
            ? this.ToastSuccessJson("Beslutning slettet.")
            : this.ToastErrorJson("Beslutningen blev ikke fundet.");

    [HttpGet]
    public async Task<IActionResult> AttachmentsPartial(int meetingId, CancellationToken cancellationToken)
    {
        var meeting = await meetingRepository.GetByIdAsync(meetingId, cancellationToken);
        if (meeting is null)
            return NotFound();

        ViewData["IsReadOnly"] = !meeting.VersionInfo.IsLatestVersion;
        return PartialView("_MeetingAttachmentsList", meeting.Attachments.Select(a => a.ToMeetingAttachmentViewModel()).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAttachment(UploadMeetingAttachmentViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.File is null || model.File.Length == 0)
            return this.ToastErrorJson("Vælg en fil.");

        await using var stream = model.File.OpenReadStream();
        var attachment = await meetingManager.AddAttachmentAsync(
            model.MeetingId, stream, model.File.FileName, model.File.ContentType, userManager.GetUserId(User), isRecording: false, cancellationToken);

        return attachment is not null
            ? this.ToastSuccessJson("Fil uploadet.")
            : this.ToastErrorJson(NotFoundMessage);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttachment(int id, CancellationToken cancellationToken) =>
        await meetingManager.DeleteAttachmentAsync(id, cancellationToken)
            ? this.ToastSuccessJson("Fil slettet.")
            : this.ToastErrorJson("Filen blev ikke fundet.");

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(int id, CancellationToken cancellationToken)
    {
        var file = await meetingManager.GetAttachmentFileAsync(id, cancellationToken);
        return file is null ? NotFound() : PhysicalFile(file.FullPath, file.ContentType, file.OriginalFileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRecording(SaveMeetingRecordingViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.Audio is null || model.Audio.Length == 0)
            return this.ToastErrorJson("Ingen lydoptagelse modtaget.");

        await using var stream = model.Audio.OpenReadStream();
        var attachment = await meetingManager.AddAttachmentAsync(
            model.MeetingId, stream, model.Audio.FileName ?? "optagelse.webm", model.Audio.ContentType, userManager.GetUserId(User), isRecording: true, cancellationToken);

        return attachment is not null
            ? this.ToastSuccessJson("Optagelse gemt.")
            : this.ToastErrorJson(NotFoundMessage);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TranscribeAttachment(int attachmentId, CancellationToken cancellationToken) =>
        await meetingManager.RequestTranscriptionAsync(attachmentId, cancellationToken) switch
        {
            RequestTranscriptionStatus.Queued => this.ToastSuccessJson("Transskription startet i baggrunden."),
            RequestTranscriptionStatus.NotLatestVersion => this.ToastErrorJson("Der findes en nyere version af dette møde — denne version er skrivebeskyttet."),
            RequestTranscriptionStatus.NotAudio => this.ToastErrorJson("Kun lydfiler kan transskriberes."),
            RequestTranscriptionStatus.AlreadyInProgress => this.ToastErrorJson("Transskription er allerede i gang for denne fil."),
            _ => this.ToastErrorJson("Filen blev ikke fundet.")
        };

    [HttpGet]
    public async Task<IActionResult> TranscriptionStatusJson(int attachmentId, CancellationToken cancellationToken)
    {
        var status = await meetingRepository.GetTranscriptionStatusAsync(attachmentId, cancellationToken);
        return status is null ? NotFound() : Json(status.ToTranscriptionStatusViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateSummary(int meetingId, CancellationToken cancellationToken)
    {
        var result = await meetingManager.GenerateSummaryAsync(meetingId, cancellationToken);
        return result.Status switch
        {
            GenerateSummaryStatus.Generated => Json(new { success = true, summary = result.Summary }),
            GenerateSummaryStatus.NoNotes => this.ToastErrorJson("Der er ingen noter at opsummere endnu."),
            GenerateSummaryStatus.NoModel => this.ToastErrorJson("Ingen AI-model er konfigureret."),
            GenerateSummaryStatus.EmptyAnswer => this.ToastErrorJson("AI-gatewayen returnerede intet referat."),
            GenerateSummaryStatus.Failed => this.ToastErrorJson(result.ErrorMessage!),
            _ => this.ToastErrorJson(NotFoundMessage)
        };
    }
}
