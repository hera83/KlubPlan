using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Data.Models;
using web.Extensions;
using web.Managers.Arrangements.Interfaces;
using web.Repositories.Arrangements.Dtos;
using web.Repositories.Arrangements.Interfaces;
using web.ViewModels;
using web.ViewModels.Mappers;

namespace web.Controllers;

[Authorize]
public class RegistrationsController(
    IArrangementRepository arrangementRepository,
    IArrangementManager arrangementManager,
    UserManager<ApplicationUser> userManager) : Controller
{
    private const string NotFoundMessage = "Arrangementet blev ikke fundet.";

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var filter = new ArrangementFilterViewModel();
        var page = await arrangementRepository.SearchAsync(filter.ToSearchArrangementsDto(), cancellationToken);
        return View(page.ToArrangementFilterViewModel(filter));
    }

    [HttpGet]
    public async Task<IActionResult> ArrangementsTable(ArrangementFilterViewModel filter, CancellationToken cancellationToken)
    {
        var page = await arrangementRepository.SearchAsync(filter.ToSearchArrangementsDto(), cancellationToken);
        return PartialView("_ArrangementsTableBody", page.ToArrangementFilterViewModel(filter));
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var builder = await arrangementManager.GetBuilderAsync(null, cancellationToken);
        return View("Builder", builder!.ToArrangementBuilderViewModel());
    }

    public Task<IActionResult> Edit(int id, CancellationToken cancellationToken) => BuilderView(id, "Builder", cancellationToken);

    public Task<IActionResult> Preview(int id, CancellationToken cancellationToken) => BuilderView(id, nameof(Preview), cancellationToken);

    public async Task<IActionResult> Registrations(int id, CancellationToken cancellationToken)
    {
        var registrations = await arrangementManager.GetRegistrationsAsync(id, page: 1, pageSize: 10, cancellationToken);
        if (registrations is null)
        {
            this.ToastError(NotFoundMessage);
            return RedirectToAction(nameof(Index));
        }

        return View(registrations.ToArrangementRegistrationsViewModel());
    }

    public async Task<IActionResult> RegistrationsTable(int id, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        var registrations = await arrangementManager.GetRegistrationsAsync(id, page ?? 1, pageSize ?? 10, cancellationToken);
        return registrations is null ? NotFound() : PartialView("_RegistrationsTableBody", registrations.ToArrangementRegistrationsViewModel());
    }

    public async Task<IActionResult> ExportRegistrations(int id, CancellationToken cancellationToken)
    {
        var registrations = await arrangementManager.GetAllRegistrationsAsync(id, cancellationToken);
        if (registrations is null)
        {
            this.ToastError(NotFoundMessage);
            return RedirectToAction(nameof(Index));
        }

        var model = registrations.ToArrangementRegistrationsViewModel();
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
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await arrangementRepository.DeleteAsync(id, cancellationToken)
            ? this.ToastSuccessJson("Arrangementet er slettet.")
            : this.ToastErrorJson(NotFoundMessage);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRegistrationOpen(int id, CancellationToken cancellationToken)
    {
        var isOpen = await arrangementRepository.ToggleRegistrationForcedOpenAsync(id, cancellationToken);
        return isOpen switch
        {
            true => this.ToastSuccessJson("Tilmeldingen er nu midlertidigt åbnet."),
            false => this.ToastSuccessJson("Tilmeldingen følger nu igen de angivne datoer."),
            null => this.ToastErrorJson(NotFoundMessage)
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ArrangementBuilderViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            this.ToastError("Arrangementet kunne ikke gemmes. Kontroller felterne.");
            return await RebuildAsync(model, cancellationToken);
        }

        var result = await arrangementRepository.SaveAsync(model.ToSaveArrangementDto(userManager.GetUserId(User)), cancellationToken);
        if (result.Status != SaveArrangementStatus.Saved)
        {
            this.ToastError(result.Status == SaveArrangementStatus.NotFound ? NotFoundMessage : result.ErrorMessage ?? "Arrangementet kunne ikke gemmes.");
            return await RebuildAsync(model, cancellationToken);
        }

        this.ToastSuccess(model.Id > 0 ? "Arrangementet er opdateret." : "Arrangementet er oprettet.");
        return RedirectToAction(nameof(Edit), new { id = result.ArrangementId });
    }

    private async Task<IActionResult> BuilderView(int id, string viewName, CancellationToken cancellationToken)
    {
        var builder = await arrangementManager.GetBuilderAsync(id, cancellationToken);
        if (builder is null)
        {
            this.ToastError(NotFoundMessage);
            return RedirectToAction(nameof(Index));
        }

        return View(viewName, builder.ToArrangementBuilderViewModel());
    }

    /// <summary>Shows the posted builder again with the option lists filled in.</summary>
    private async Task<IActionResult> RebuildAsync(ArrangementBuilderViewModel model, CancellationToken cancellationToken)
    {
        var options = await arrangementManager.GetBuilderAsync(null, cancellationToken);
        model.ApplyOptions(options!);
        return View("Builder", model);
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
