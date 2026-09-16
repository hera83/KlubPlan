using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Infrastructure;
using web.Repositories.People.Dtos;
using web.Repositories.People.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    [Authorize]
    public class PeopleController : Controller
    {
        private readonly IPeopleService _peopleService;

        public PeopleController(IPeopleService peopleService)
        {
            _peopleService = peopleService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _peopleService.GetPeopleAsync(new PersonFilterViewModel(), HttpContext.RequestAborted);
            ViewData["Groups"] = await _peopleService.GetGroupsAsync(HttpContext.RequestAborted);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> PeopleTable(PersonFilterViewModel filter)
        {
            var model = await _peopleService.GetPeopleAsync(filter, HttpContext.RequestAborted);
            return PartialView("_PeopleTableBody", model);
        }

        [HttpGet]
        public async Task<IActionResult> PersonDetails(int id)
        {
            var detail = await _peopleService.GetPersonDetailAsync(id, HttpContext.RequestAborted);
            if (detail is null)
                return NotFound();

            return Json(detail);
        }

        [HttpGet]
        public async Task<IActionResult> GroupsManageList()
        {
            var groups = await _peopleService.GetGroupsAsync(HttpContext.RequestAborted);
            return PartialView("_PersonGroupsManageList", groups);
        }

        [HttpGet]
        public async Task<IActionResult> GroupOptionsJson()
        {
            var groups = await _peopleService.GetGroupsAsync(HttpContext.RequestAborted);
            return Json(groups.Select(g => new { g.Id, g.Name }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePerson(CreatePersonViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Personen kunne ikke oprettes. Kontroller felterne.");

            var result = await _peopleService.CreatePersonAsync(new CreatePersonRequestDto
            {
                Uid = model.Uid,
                Name = model.Name,
                BirthDate = model.BirthDate,
                Mobile = model.Mobile,
                Email = model.Email,
                GroupIds = model.GroupIds,
                Guardians = model.Guardians
            }, HttpContext.RequestAborted);

            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Personen kunne ikke oprettes.");

            return this.ToastSuccessJson($"Person oprettet (UID {result.GeneratedUid}).");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPerson(EditPersonViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Personen kunne ikke gemmes. Kontroller felterne.");

            var result = await _peopleService.UpdatePersonAsync(new UpdatePersonRequestDto
            {
                Id = model.Id,
                Uid = model.Uid,
                Name = model.Name,
                BirthDate = model.BirthDate,
                Mobile = model.Mobile,
                Email = model.Email,
                GroupIds = model.GroupIds,
                Guardians = model.Guardians
            }, HttpContext.RequestAborted);

            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Personen kunne ikke gemmes.");

            return this.ToastSuccessJson("Person opdateret.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePerson(int id)
        {
            var deleted = await _peopleService.DeletePersonAsync(id, HttpContext.RequestAborted);
            return deleted
                ? this.ToastSuccessJson("Person slettet.")
                : this.ToastErrorJson("Personen blev ikke fundet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePersonGroup(CreatePersonGroupViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Gruppen kunne ikke oprettes. Angiv et navn.");

            var result = await _peopleService.CreateGroupAsync(model.Name, HttpContext.RequestAborted);
            return result.Success
                ? this.ToastSuccessJson("Gruppe oprettet.")
                : this.ToastErrorJson(result.ErrorMessage ?? "Gruppen kunne ikke oprettes.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPersonGroup(EditPersonGroupViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Gruppen kunne ikke gemmes. Angiv et navn.");

            var result = await _peopleService.UpdateGroupAsync(model.Id, model.Name, HttpContext.RequestAborted);
            return result.Success
                ? this.ToastSuccessJson("Gruppe opdateret.")
                : this.ToastErrorJson(result.ErrorMessage ?? "Gruppen kunne ikke gemmes.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePersonGroup(int id)
        {
            var result = await _peopleService.DeleteGroupAsync(id, HttpContext.RequestAborted);
            return result.Success
                ? this.ToastSuccessJson("Gruppe slettet.")
                : this.ToastErrorJson(result.ErrorMessage ?? "Gruppen blev ikke fundet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportPersonsToGroup(ImportPersonsToGroupViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Vælg en gruppe og angiv mindst ét navn.");

            var names = model.RawNames.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();

            var result = await _peopleService.ImportPersonsToGroupAsync(model.GroupId, names, HttpContext.RequestAborted);

            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Import mislykkedes.");

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
    }
}
