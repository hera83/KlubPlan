using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Managers.Arrangements.Interfaces;
using web.ViewModels;
using web.ViewModels.Mappers;

namespace web.Controllers;

/// <summary>
/// Public, unauthenticated arrangement sign-up links: /Tilmelding?Id={arrangement.PublicId}&amp;
/// UId={person.PublicId}. Mirrors FormularController's public /Formular link, with one key
/// difference: Tilmelding has no anonymous mode, so UId is always required. Id is
/// Arrangement.PublicId (not its internal, sequential Id) so hopping between arrangements one
/// isn't meant to see isn't possible by guessing.
/// </summary>
[AllowAnonymous]
public class TilmeldingController(IArrangementManager arrangementManager) : Controller
{
    public async Task<IActionResult> Index(Guid id, Guid? uId, CancellationToken cancellationToken)
    {
        var publicArrangement = await arrangementManager.GetPublicAsync(id, uId, cancellationToken);
        return View(publicArrangement.ToPublicArrangementAccessViewModel(uId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        Guid id,
        Guid? uId,
        List<ArrangementAnswerInputViewModel> answers,
        List<int> shiftIds,
        List<int> confirmedRequirementIds,
        List<ArrangementShiftCompanionInputViewModel> companions,
        CancellationToken cancellationToken)
    {
        var input = answers.ToSubmitPublicArrangementDto(shiftIds, confirmedRequirementIds, companions);
        var result = await arrangementManager.SubmitPublicAsync(id, uId, input, cancellationToken);

        if (result.Status == PublicArrangementStatus.Ok && result.Success)
        {
            return View("Index", new PublicArrangementAccessViewModel
            {
                Status = PublicArrangementStatus.Ok,
                Registered = true,
                Arrangement = new ArrangementFillViewModel { Title = result.ArrangementTitle ?? string.Empty }
            });
        }

        // The gate can fail here even though the GET succeeded (e.g. someone else used the same
        // link in between, or registration closed) — re-resolve for a fresh, accurate view.
        var model = (await arrangementManager.GetPublicAsync(id, uId, cancellationToken)).ToPublicArrangementAccessViewModel(uId);
        if (result.Status != PublicArrangementStatus.Ok)
            return View("Index", model);

        model.Arrangement?.ApplySubmitted(input);
        model.FieldErrors = result.FieldErrors;
        model.ShiftError = result.ShiftError;
        return View("Index", model);
    }
}
