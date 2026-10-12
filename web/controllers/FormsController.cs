using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Models;
using web.Extensions;
using web.Managers.Forms.Dtos;
using web.Managers.Forms.Interfaces;
using web.Repositories.Forms.Dtos;
using web.Repositories.Forms.Interfaces;
using web.ViewModels;
using web.ViewModels.Mappers;

namespace web.Controllers;

[Authorize]
public class FormsController(
    IFormRepository formRepository,
    IFormManager formManager,
    UserManager<ApplicationUser> userManager) : Controller
{
    private const string NotFoundMessage = "Formularen blev ikke fundet.";
    private const string NotLatestVersionMessage = "Der findes en nyere version af denne formular — kun den aktuelle version kan redigeres.";

    private bool IsAdmin => User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.Developer);

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var filter = new FormFilterViewModel();
        var page = await formRepository.SearchAsync(filter.ToSearchFormsDto(), userManager.GetUserId(User) ?? string.Empty, cancellationToken);
        return View(page.ToFormFilterViewModel(filter, IsAdmin));
    }

    public async Task<IActionResult> FormsTable(FormFilterViewModel filter, CancellationToken cancellationToken)
    {
        var page = await formRepository.SearchAsync(filter.ToSearchFormsDto(), userManager.GetUserId(User) ?? string.Empty, cancellationToken);
        return PartialView("_FormsTableBody", page.ToFormFilterViewModel(filter, IsAdmin));
    }

    [Authorize(Policy = "AdminOrDeveloper")]
    public IActionResult Create()
    {
        var model = new FormBuilderViewModel
        {
            Fields = [new() { FieldType = FormFieldType.SectionHeading, Order = 0 }]
        };
        return View("Builder", model);
    }

    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var form = await formRepository.GetByIdAsync(id, cancellationToken);
        if (form is null)
        {
            this.ToastError(NotFoundMessage);
            return RedirectToAction(nameof(Index));
        }

        var versionInfo = await formRepository.GetVersionInfoAsync(id, cancellationToken);
        return View("Builder", form.ToFormBuilderViewModel(versionInfo!));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> Save(FormBuilderViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            this.ToastError("Formularen kunne ikke gemmes. Kontroller felterne.");
            return View("Builder", model);
        }

        var result = await formRepository.SaveAsync(model.ToSaveFormDto(userManager.GetUserId(User)), cancellationToken);
        if (result.Status != SaveFormStatus.Saved)
        {
            this.ToastError(SaveErrorMessage(result) ?? "Formularen kunne ikke gemmes.");
            return View("Builder", model);
        }

        this.ToastSuccess(model.Id > 0 ? "Formularen er opdateret." : "Formularen er oprettet.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await formRepository.DeleteAsync(id, cancellationToken)
            ? this.ToastSuccessJson("Formularen er slettet.")
            : this.ToastErrorJson(NotFoundMessage);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> CreateNewVersion(int id, CancellationToken cancellationToken)
    {
        var result = await formRepository.CreateNewVersionAsync(id, userManager.GetUserId(User), cancellationToken);
        return result.Status switch
        {
            CreateFormVersionStatus.Created => Json(new { success = true, message = "Ny version oprettet.", type = "success", formId = result.FormId }),
            CreateFormVersionStatus.StillAcceptingResponses => this.ToastErrorJson("Der kan kun oprettes en ny version, når formularen er lukket for svar."),
            CreateFormVersionStatus.NotLatestVersion => this.ToastErrorJson("Der findes allerede en nyere version af denne formular."),
            _ => this.ToastErrorJson(NotFoundMessage)
        };
    }

    public Task<IActionResult> Preview(int id, CancellationToken cancellationToken) => FillView(id, cancellationToken);

    public Task<IActionResult> Fill(int id, CancellationToken cancellationToken) => FillView(id, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id, List<FormAnswerInputViewModel> answers, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return Challenge();

        var input = answers.ToFormAnswerInputDtos();
        var result = await formManager.SubmitAsync(id, userId, input, cancellationToken);
        if (result.Status == SubmitFormStatus.Submitted)
        {
            this.ToastSuccess("Tak for din besvarelse!");
            return RedirectToAction(nameof(Index));
        }

        var model = await GetFillViewModelAsync(id, cancellationToken);
        if (model is null)
        {
            this.ToastError(NotFoundMessage);
            return RedirectToAction(nameof(Index));
        }

        // Behold det respondenten allerede har tastet ind, og marker de felter der mangler.
        model.ApplySubmittedAnswers(input);
        this.ToastError(result.Status switch
        {
            SubmitFormStatus.Invalid => "Udfyld venligst de påkrævede felter.",
            SubmitFormStatus.Closed => "Formularen tager ikke længere imod svar.",
            _ => NotFoundMessage
        });

        ViewBag.FieldErrors = result.FieldErrors;
        return View("Fill", model);
    }

    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> Responses(int id, CancellationToken cancellationToken)
    {
        var responses = await formManager.GetResponsesAsync(id, page: 1, pageSize: 10, cancellationToken);
        if (responses is null)
        {
            this.ToastError(NotFoundMessage);
            return RedirectToAction(nameof(Index));
        }

        return View(responses.ToFormResponsesViewModel());
    }

    public async Task<IActionResult> ResponsesTable(int id, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        var responses = await formManager.GetResponsesAsync(id, page ?? 1, pageSize ?? 10, cancellationToken);
        return responses is null ? NotFound() : PartialView("_ResponsesTableBody", responses.ToFormResponsesViewModel());
    }

    [Authorize(Policy = "AdminOrDeveloper")]
    public async Task<IActionResult> ExportResponses(int id, CancellationToken cancellationToken)
    {
        var responses = await formManager.GetAllResponsesAsync(id, cancellationToken);
        if (responses is null)
        {
            this.ToastError(NotFoundMessage);
            return RedirectToAction(nameof(Index));
        }

        var sb = new StringBuilder();
        var headers = new List<string> { "Besvaret af", "Dato" }.Concat(responses.Columns.Select(c => c.Label));
        sb.AppendLine(string.Join(";", headers.Select(CsvEscape)));

        foreach (var row in responses.Rows)
        {
            var cells = new List<string> { row.SubmittedByDisplayName, row.SubmittedAtUtc.ToLocalTime().ToDanishDateTime() };
            cells.AddRange(responses.Columns.Select(c => row.Answers.TryGetValue(c.FormFieldId, out var v) ? v : string.Empty));
            sb.AppendLine(string.Join(";", cells.Select(CsvEscape)));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var safeTitle = string.Concat(responses.FormTitle.Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-')).Trim().Replace(' ', '-');
        var fileName = $"{(string.IsNullOrWhiteSpace(safeTitle) ? "formular" : safeTitle)}-svar.csv";

        return File(bytes, "text/csv", fileName);
    }

    [Authorize(Policy = "AdminOrDeveloper")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAcceptingResponses(int id, CancellationToken cancellationToken)
    {
        var form = await formRepository.GetByIdAsync(id, cancellationToken);
        if (form is null)
            return this.ToastErrorJson(NotFoundMessage);

        var accepting = !form.IsAcceptingResponses;
        var result = await formRepository.SetAcceptingResponsesAsync(id, accepting, cancellationToken);
        return result.Status == SaveFormStatus.Saved
            ? this.ToastSuccessJson(accepting ? "Formularen tager nu imod svar." : "Formularen tager ikke længere imod svar.")
            : this.ToastErrorJson(SaveErrorMessage(result) ?? "Kunne ikke opdatere formularen.");
    }

    private async Task<IActionResult> FillView(int id, CancellationToken cancellationToken)
    {
        var model = await GetFillViewModelAsync(id, cancellationToken);
        if (model is null)
        {
            this.ToastError(NotFoundMessage);
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    /// <summary>The fill-out form; only open when the form accepts responses and is the latest version.</summary>
    private async Task<FormFillViewModel?> GetFillViewModelAsync(int id, CancellationToken cancellationToken)
    {
        var form = await formRepository.GetByIdAsync(id, cancellationToken);
        if (form is null)
            return null;

        var isLatest = await formRepository.IsLatestVersionAsync(id, cancellationToken);
        return form.ToFormFillViewModel(form.IsAcceptingResponses && isLatest);
    }

    private static string? SaveErrorMessage(SaveFormResultDto result) => result.Status switch
    {
        SaveFormStatus.NotFound => NotFoundMessage,
        SaveFormStatus.NotLatestVersion => NotLatestVersionMessage,
        _ => result.ErrorMessage
    };

    private static string CsvEscape(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
