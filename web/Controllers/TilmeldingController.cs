using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Repositories.Registrations.Dtos;
using web.Repositories.Registrations.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    /// <summary>
    /// Public, unauthenticated arrangement sign-up links: /Tilmelding?Id={arrangement.PublicId}&amp;
    /// UId={person.PublicId}. Mirrors FormularController's public /Formular link, with one key
    /// difference: Tilmelding has no anonymous mode, so UId is always required, not just when the
    /// arrangement isn't anonymous. Id is Arrangement.PublicId (not its internal, sequential Id) so
    /// hopping between arrangements one isn't meant to see isn't possible by guessing.
    /// </summary>
    [AllowAnonymous]
    public class TilmeldingController : Controller
    {
        private readonly IArrangementService _arrangementService;

        public TilmeldingController(IArrangementService arrangementService)
        {
            _arrangementService = arrangementService;
        }

        public async Task<IActionResult> Index(Guid id, Guid? uId, CancellationToken ct)
        {
            var model = await _arrangementService.GetPublicArrangementAsync(id, uId, ct);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Guid id, Guid? uId, List<ArrangementAnswerInputViewModel> answers, List<int> shiftIds, List<int> confirmedRequirementIds, CancellationToken ct)
        {
            var dto = new SubmitPublicArrangementRequestDto
            {
                ArrangementPublicId = id,
                PersonPublicId = uId,
                Answers = (answers ?? new List<ArrangementAnswerInputViewModel>()).Select(a => new SubmitArrangementAnswerDto
                {
                    ArrangementFormFieldId = a.ArrangementFormFieldId,
                    Value = a.Value,
                    Values = a.Values
                }).ToList(),
                SelectedShiftIds = shiftIds ?? new List<int>(),
                ConfirmedRequirementIds = confirmedRequirementIds ?? new List<int>()
            };

            var result = await _arrangementService.SubmitPublicArrangementAsync(dto, ct);

            if (result.Status == PublicArrangementStatus.Ok && result.Success)
            {
                return View("Index", new PublicArrangementAccessViewModel
                {
                    Status = PublicArrangementStatus.Ok,
                    Registered = true,
                    Arrangement = new ArrangementFillViewModel { Title = result.ArrangementTitle ?? string.Empty }
                });
            }

            // The gate can fail here even though the GET succeeded (e.g. someone else used the
            // same link in between, or the arrangement was closed) — re-resolve for a fresh,
            // accurate view.
            if (result.Status != PublicArrangementStatus.Ok)
            {
                var gate = await _arrangementService.GetPublicArrangementAsync(id, uId, ct);
                return View("Index", gate);
            }

            var model = await _arrangementService.GetPublicArrangementAsync(id, uId, ct);
            if (model.Arrangement is not null)
            {
                var answerLookup = dto.Answers.ToDictionary(a => a.ArrangementFormFieldId);
                foreach (var field in model.Arrangement.Fields)
                {
                    if (!answerLookup.TryGetValue(field.ArrangementFormFieldId, out var answer)) continue;
                    field.SubmittedValue = answer.Value;
                    field.SubmittedValues = answer.Values;
                }

                foreach (var shift in model.Arrangement.Shifts)
                {
                    shift.IsSelected = dto.SelectedShiftIds.Contains(shift.Id);
                }
            }

            model.FieldErrors = result.FieldErrors;
            model.ShiftError = result.ShiftError;
            return View("Index", model);
        }
    }
}
