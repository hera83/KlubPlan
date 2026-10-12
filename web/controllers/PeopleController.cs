using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Extensions;
using web.Managers.People.Dtos;
using web.Managers.People.Interfaces;
using web.Repositories.People.Dtos;
using web.Repositories.People.Interfaces;
using web.Repositories.PersonGroups.Dtos;
using web.Repositories.PersonGroups.Interfaces;
using web.ViewModels;
using web.ViewModels.Mappers;

namespace web.Controllers;

[Authorize]
public class PeopleController(
    IPersonRepository personRepository,
    IPersonGroupRepository personGroupRepository,
    IPeopleManager peopleManager) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var filter = new PersonFilterViewModel();
        var overview = await peopleManager.GetOverviewAsync(filter.ToSearchPeopleDto(), cancellationToken);
        ViewData["Groups"] = overview.Groups.Select(g => g.ToPersonGroupViewModel()).ToList();
        return View(overview.ToPersonFilterViewModel(filter));
    }

    [HttpGet]
    public async Task<IActionResult> PeopleTable(PersonFilterViewModel filter, CancellationToken cancellationToken)
    {
        var overview = await peopleManager.GetOverviewAsync(filter.ToSearchPeopleDto(), cancellationToken);
        return PartialView("_PeopleTableBody", overview.ToPersonFilterViewModel(filter));
    }

    [HttpGet]
    public async Task<IActionResult> PersonDetails(int id, CancellationToken cancellationToken)
    {
        var person = await personRepository.GetByIdAsync(id, cancellationToken);
        if (person is null)
        {
            return NotFound();
        }

        return Json(person.ToPersonDetailViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> GroupsManageList(CancellationToken cancellationToken)
    {
        var groups = await personGroupRepository.GetAllAsync(cancellationToken);
        return PartialView("_PersonGroupsManageList", groups.Select(g => g.ToPersonGroupViewModel()).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> GroupOptionsJson(CancellationToken cancellationToken)
    {
        var groups = await personGroupRepository.GetAllAsync(cancellationToken);
        return Json(groups.Select(g => new { g.Id, g.Name }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePerson(CreatePersonViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return this.ToastErrorJson("Personen kunne ikke oprettes. Kontroller felterne.");
        }

        var result = await personRepository.CreateAsync(model.ToCreatePersonDto(), cancellationToken);
        return result.Status == SavePersonStatus.Saved
            ? this.ToastSuccessJson($"Person oprettet (UID {result.Uid}).")
            : this.ToastErrorJson(UidInUseMessage(result.Uid));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPerson(EditPersonViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return this.ToastErrorJson("Personen kunne ikke gemmes. Kontroller felterne.");
        }

        var result = await personRepository.UpdateAsync(model.Id, model.ToUpdatePersonDto(), cancellationToken);
        return result.Status switch
        {
            SavePersonStatus.Saved => this.ToastSuccessJson("Person opdateret."),
            SavePersonStatus.NotFound => this.ToastErrorJson("Personen blev ikke fundet."),
            _ => this.ToastErrorJson(UidInUseMessage(result.Uid))
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePerson(int id, CancellationToken cancellationToken) =>
        await personRepository.DeleteAsync(id, cancellationToken)
            ? this.ToastSuccessJson("Person slettet.")
            : this.ToastErrorJson("Personen blev ikke fundet.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegeneratePersonPublicId(int id, CancellationToken cancellationToken) =>
        await personRepository.RegeneratePublicIdAsync(id, cancellationToken)
            ? this.ToastSuccessJson("Nyt link-id genereret. Tidligere udleverede formular-links til denne person virker ikke længere.")
            : this.ToastErrorJson("Personen blev ikke fundet.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePersonGroup(CreatePersonGroupViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return this.ToastErrorJson("Gruppen kunne ikke oprettes. Angiv et navn.");
        }

        var result = await personGroupRepository.CreateAsync(model.Name, cancellationToken);
        return result.Status == SavePersonGroupStatus.Saved
            ? this.ToastSuccessJson("Gruppe oprettet.")
            : this.ToastErrorJson(GroupNameInUseMessage(result.Name));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPersonGroup(EditPersonGroupViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return this.ToastErrorJson("Gruppen kunne ikke gemmes. Angiv et navn.");
        }

        var result = await personGroupRepository.RenameAsync(model.Id, model.Name, cancellationToken);
        return result.Status switch
        {
            SavePersonGroupStatus.Saved => this.ToastSuccessJson("Gruppe opdateret."),
            SavePersonGroupStatus.NotFound => this.ToastErrorJson("Gruppen blev ikke fundet."),
            _ => this.ToastErrorJson(GroupNameInUseMessage(result.Name))
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePersonGroup(int id, CancellationToken cancellationToken) =>
        await personGroupRepository.DeleteAsync(id, cancellationToken)
            ? this.ToastSuccessJson("Gruppe slettet.")
            : this.ToastErrorJson("Gruppen blev ikke fundet.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportPersonsToGroup(ImportPersonsToGroupViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return this.ToastErrorJson("Vælg en gruppe og angiv mindst ét navn.");
        }

        var names = model.RawNames.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var result = await peopleManager.ImportToGroupAsync(model.GroupId, names, cancellationToken);

        switch (result.Status)
        {
            case ImportPersonsStatus.GroupNotFound:
                return this.ToastErrorJson("Gruppen blev ikke fundet.");
            case ImportPersonsStatus.NoNames:
                return this.ToastErrorJson("Angiv mindst ét navn.");
            case ImportPersonsStatus.Failed:
                return this.ToastErrorJson("Importen kunne ikke gennemføres. Prøv igen.");
        }

        var hasErrors = result.Errors.Count > 0;
        var message = hasErrors
            ? $"{result.ImportedCount} person(er) importeret, {result.Errors.Count} linje(r) kunne ikke importeres."
            : $"{result.ImportedCount} person(er) importeret.";

        return Json(new
        {
            success = true,
            type = hasErrors ? "warning" : "success",
            message,
            importedCount = result.ImportedCount,
            errors = result.Errors.Select(e => new { e.Name, e.Reason })
        });
    }

    private static string UidInUseMessage(string uid) => $"UID '{uid}' er allerede i brug. Vælg et andet.";

    private static string GroupNameInUseMessage(string name) => $"Gruppen '{name}' findes allerede.";
}
