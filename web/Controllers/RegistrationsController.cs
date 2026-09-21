using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.Registrations.Dtos;
using web.Repositories.Registrations.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    [Authorize]
    public class RegistrationsController : Controller
    {
        private readonly IArrangementService _arrangementService;
        private readonly UserManager<ApplicationUser> _userManager;

        public RegistrationsController(IArrangementService arrangementService, UserManager<ApplicationUser> userManager)
        {
            _arrangementService = arrangementService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var model = await _arrangementService.GetArrangementsAsync(new ArrangementFilterViewModel(), ct);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ArrangementsTable(ArrangementFilterViewModel filter, CancellationToken ct)
        {
            var model = await _arrangementService.GetArrangementsAsync(filter, ct);
            return PartialView("_ArrangementsTableBody", model);
        }

        public async Task<IActionResult> Create(CancellationToken ct)
        {
            return View("Builder", await _arrangementService.GetBuilderShellAsync(ct));
        }

        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var model = await _arrangementService.GetArrangementForBuilderAsync(id, ct);
            if (model is null)
            {
                this.ToastError("Arrangementet blev ikke fundet.");
                return RedirectToAction(nameof(Index));
            }

            return View("Builder", model);
        }

        public async Task<IActionResult> Preview(int id, CancellationToken ct)
        {
            var model = await _arrangementService.GetArrangementForBuilderAsync(id, ct);
            if (model is null)
            {
                this.ToastError("Arrangementet blev ikke fundet.");
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> Registrations(int id, CancellationToken ct)
        {
            var model = await _arrangementService.GetArrangementRegistrationsAsync(id, page: 1, pageSize: 10, ct);
            if (model is null)
            {
                this.ToastError("Arrangementet blev ikke fundet.");
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> RegistrationsTable(int id, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var model = await _arrangementService.GetArrangementRegistrationsAsync(id, page, pageSize, ct);
            if (model is null) return NotFound();

            return PartialView("_RegistrationsTableBody", model);
        }

        public async Task<IActionResult> ExportRegistrations(int id, CancellationToken ct)
        {
            var model = await _arrangementService.GetAllArrangementRegistrationsForExportAsync(id, ct);
            if (model is null)
            {
                this.ToastError("Arrangementet blev ikke fundet.");
                return RedirectToAction(nameof(Index));
            }

            var sb = new StringBuilder();
            var headers = new List<string> { "Navn", "Tilmeldt dato", "Vagter" }.Concat(model.Columns.Select(c => c.Label));
            sb.AppendLine(string.Join(";", headers.Select(CsvEscape)));

            foreach (var row in model.Rows)
            {
                var cells = new List<string> { row.PersonName, row.RegisteredAtUtc.ToLocalTime().ToDanishDateTime(), string.Join(", ", row.ShiftLabels) };
                cells.AddRange(model.Columns.Select(c => row.Answers.TryGetValue(c.ArrangementFormFieldId, out var v) ? v : string.Empty));
                sb.AppendLine(string.Join(";", cells.Select(CsvEscape)));
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            var safeTitle = string.Concat(model.ArrangementTitle.Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-')).Trim().Replace(' ', '-');
            var fileName = $"{(string.IsNullOrWhiteSpace(safeTitle) ? "tilmelding" : safeTitle)}-tilmeldte.csv";

            return File(bytes, "text/csv", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var success = await _arrangementService.DeleteArrangementAsync(id, ct);
            return success
                ? this.ToastSuccessJson("Arrangementet er slettet.")
                : this.ToastErrorJson("Arrangementet blev ikke fundet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRegistrationOpen(int id, CancellationToken ct)
        {
            var result = await _arrangementService.ToggleRegistrationOpenAsync(id, ct);
            return result.Success
                ? this.ToastSuccessJson(result.IsOpen ? "Tilmeldingen er nu midlertidigt åbnet." : "Tilmeldingen følger nu igen de angivne datoer.")
                : this.ToastErrorJson(result.ErrorMessage ?? "Kunne ikke opdatere tilmeldingen.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(ArrangementBuilderViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                this.ToastError("Arrangementet kunne ikke gemmes. Kontroller felterne.");
                await RepopulateOptionsAsync(model, ct);
                return View("Builder", model);
            }

            var dto = new SaveArrangementRequestDto
            {
                Id = model.Id,
                Title = model.Title,
                Description = model.Description,
                UserId = _userManager.GetUserId(User),
                FormFields = model.FormFields.Select(f => new SaveArrangementFormFieldDto
                {
                    Id = f.Id,
                    Label = f.Label,
                    HelpText = f.HelpText,
                    FieldType = f.FieldType,
                    IsRequired = f.IsRequired,
                    Order = f.Order,
                    OptionsJson = f.OptionsJson
                }).ToList(),
                Shifts = model.Shifts.Select(s => new SaveArrangementShiftDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    Start = s.Start,
                    End = s.End,
                    Location = s.Location,
                    NeededCount = s.NeededCount,
                    Order = s.Order,
                    Requirements = s.Requirements.Select(r => new SaveArrangementShiftRequirementDto
                    {
                        Id = r.Id,
                        Text = r.Text,
                        Order = r.Order
                    }).ToList()
                }).ToList(),
                RegistrationOpensAtUtc = model.RegistrationOpensAt,
                RegistrationClosesAtUtc = model.RegistrationClosesAt,
                AccessMode = model.AccessMode,
                AllowedPersonIds = model.AllowedPersonIds,
                AllowedGroupIds = model.AllowedGroupIds,
                AllowMultipleNamesPerShift = model.AllowMultipleNamesPerShift
            };

            var result = await _arrangementService.SaveArrangementAsync(dto, ct);
            if (!result.Success)
            {
                this.ToastError(result.ErrorMessage ?? "Arrangementet kunne ikke gemmes.");
                await RepopulateOptionsAsync(model, ct);
                return View("Builder", model);
            }

            this.ToastSuccess(model.Id > 0 ? "Arrangementet er opdateret." : "Arrangementet er oprettet.");
            return RedirectToAction(nameof(Edit), new { id = result.ArrangementId });
        }

        private async Task RepopulateOptionsAsync(ArrangementBuilderViewModel model, CancellationToken ct)
        {
            model.GroupOptions = await _arrangementService.GetGroupOptionsAsync(ct);
            model.PersonOptions = await _arrangementService.GetPersonOptionsAsync(ct);
        }

        private static string CsvEscape(string value)
        {
            if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
