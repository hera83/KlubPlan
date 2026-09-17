using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.Forms.Dtos;
using web.Repositories.Forms.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    [Authorize]
    public class FormsController : Controller
    {
        private readonly IFormService _formService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<FormsController> _logger;

        public FormsController(IFormService formService, UserManager<ApplicationUser> userManager, ILogger<FormsController> logger)
        {
            _formService = formService;
            _userManager = userManager;
            _logger = logger;
        }

        private bool IsAdmin => User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.Developer);

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var userId = _userManager.GetUserId(User) ?? string.Empty;
            var model = await _formService.GetFormsAsync(new FormFilterViewModel(), userId, IsAdmin, ct);
            return View(model);
        }

        public async Task<IActionResult> FormsTable(FormFilterViewModel filter, CancellationToken ct)
        {
            var userId = _userManager.GetUserId(User) ?? string.Empty;
            var model = await _formService.GetFormsAsync(filter, userId, IsAdmin, ct);
            return PartialView("_FormsTableBody", model);
        }

        [Authorize(Policy = "AdminOrDeveloper")]
        public IActionResult Create()
        {
            var model = new FormBuilderViewModel
            {
                Fields = new List<FormFieldBuilderViewModel>
                {
                    new() { FieldType = FormFieldType.SectionHeading, Order = 0 }
                }
            };
            return View("Builder", model);
        }

        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var model = await _formService.GetFormForBuilderAsync(id, ct);
            if (model is null)
            {
                this.ToastError("Formularen blev ikke fundet.");
                return RedirectToAction(nameof(Index));
            }

            return View("Builder", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Save(FormBuilderViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                this.ToastError("Formularen kunne ikke gemmes. Kontroller felterne.");
                return View("Builder", model);
            }

            var dto = new SaveFormRequestDto
            {
                Id = model.Id,
                Title = model.Title,
                Description = model.Description,
                IsAcceptingResponses = model.IsAcceptingResponses,
                UserId = _userManager.GetUserId(User),
                Fields = model.Fields.Select(f => new SaveFormFieldDto
                {
                    Id = f.Id,
                    Label = f.Label,
                    HelpText = f.HelpText,
                    FieldType = f.FieldType,
                    IsRequired = f.IsRequired,
                    Order = f.Order,
                    OptionsJson = f.OptionsJson
                }).ToList()
            };

            var result = await _formService.SaveFormAsync(dto, ct);
            if (!result.Success)
            {
                this.ToastError(result.ErrorMessage ?? "Formularen kunne ikke gemmes.");
                return View("Builder", model);
            }

            this.ToastSuccess(model.Id > 0 ? "Formularen er opdateret." : "Formularen er oprettet.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var success = await _formService.DeleteFormAsync(id, ct);
            return success
                ? this.ToastSuccessJson("Formularen er slettet.")
                : this.ToastErrorJson("Formularen blev ikke fundet.");
        }

        public async Task<IActionResult> Fill(int id, CancellationToken ct)
        {
            var model = await _formService.GetFormForFillAsync(id, ct);
            if (model is null)
            {
                this.ToastError("Formularen blev ikke fundet.");
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id, List<FormAnswerInputViewModel> answers, CancellationToken ct)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var dto = new SubmitFormRequestDto
            {
                FormId = id,
                UserId = userId,
                Answers = (answers ?? new List<FormAnswerInputViewModel>()).Select(a => new SubmitFormAnswerDto
                {
                    FormFieldId = a.FormFieldId,
                    Value = a.Value,
                    Values = a.Values
                }).ToList()
            };

            var result = await _formService.SubmitFormAsync(dto, ct);
            if (!result.Success)
            {
                var model = await _formService.GetFormForFillAsync(id, ct);
                if (model is null)
                {
                    this.ToastError("Formularen blev ikke fundet.");
                    return RedirectToAction(nameof(Index));
                }

                // Behold det respondenten allerede har tastet ind, og marker de felter der mangler.
                var answerLookup = dto.Answers.ToDictionary(a => a.FormFieldId);
                foreach (var field in model.Fields)
                {
                    if (!answerLookup.TryGetValue(field.FormFieldId, out var answer)) continue;
                    field.SubmittedValue = answer.Value;
                    field.SubmittedValues = answer.Values;
                }

                if (result.FieldErrors.Count > 0)
                {
                    this.ToastError("Udfyld venligst de påkrævede felter.");
                }
                else
                {
                    this.ToastError(result.ErrorMessage ?? "Din besvarelse kunne ikke gemmes.");
                }

                ViewBag.FieldErrors = result.FieldErrors;
                return View("Fill", model);
            }

            this.ToastSuccess("Tak for din besvarelse!");
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Responses(int id, CancellationToken ct)
        {
            var model = await _formService.GetResponsesAsync(id, page: 1, pageSize: 10, ct);
            if (model is null)
            {
                this.ToastError("Formularen blev ikke fundet.");
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> ResponsesTable(int id, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var model = await _formService.GetResponsesAsync(id, page, pageSize, ct);
            if (model is null) return NotFound();

            return PartialView("_ResponsesTableBody", model);
        }

        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> ExportResponses(int id, CancellationToken ct)
        {
            var model = await _formService.GetAllResponsesForExportAsync(id, ct);
            if (model is null)
            {
                this.ToastError("Formularen blev ikke fundet.");
                return RedirectToAction(nameof(Index));
            }

            var sb = new StringBuilder();
            var headers = new List<string> { "Besvaret af", "Dato" }.Concat(model.Columns.Select(c => c.Label));
            sb.AppendLine(string.Join(";", headers.Select(CsvEscape)));

            foreach (var row in model.Rows)
            {
                var cells = new List<string> { row.SubmittedByDisplayName, row.SubmittedAtUtc.ToLocalTime().ToString("g") };
                cells.AddRange(model.Columns.Select(c => row.Answers.TryGetValue(c.FormFieldId, out var v) ? v : string.Empty));
                sb.AppendLine(string.Join(";", cells.Select(CsvEscape)));
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            var safeTitle = string.Concat(model.FormTitle.Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-')).Trim().Replace(' ', '-');
            var fileName = $"{(string.IsNullOrWhiteSpace(safeTitle) ? "formular" : safeTitle)}-svar.csv";

            return File(bytes, "text/csv", fileName);
        }

        [Authorize(Policy = "AdminOrDeveloper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAcceptingResponses(int id, CancellationToken ct)
        {
            var model = await _formService.GetFormForBuilderAsync(id, ct);
            if (model is null)
                return this.ToastErrorJson("Formularen blev ikke fundet.");

            var dto = new SaveFormRequestDto
            {
                Id = model.Id,
                Title = model.Title,
                Description = model.Description,
                IsAcceptingResponses = !model.IsAcceptingResponses,
                Fields = model.Fields.Select(f => new SaveFormFieldDto
                {
                    Id = f.Id,
                    Label = f.Label,
                    HelpText = f.HelpText,
                    FieldType = f.FieldType,
                    IsRequired = f.IsRequired,
                    Order = f.Order,
                    OptionsJson = f.OptionsJson
                }).ToList()
            };

            var result = await _formService.SaveFormAsync(dto, ct);
            return result.Success
                ? this.ToastSuccessJson(dto.IsAcceptingResponses ? "Formularen tager nu imod svar." : "Formularen tager ikke længere imod svar.")
                : this.ToastErrorJson(result.ErrorMessage ?? "Kunne ikke opdatere formularen.");
        }

        private static string CsvEscape(string value)
        {
            if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
