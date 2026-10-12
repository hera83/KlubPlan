using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Managers.Forms.Interfaces;
using web.ViewModels;
using web.ViewModels.Mappers;

namespace web.Controllers;

/// <summary>
/// Public, unauthenticated form links: /Formular?Id={form.PublicId}&amp;UId={person.PublicId}.
/// Lets a form be sent to a group of People (data/Models/Person) who don't have — and don't
/// need — a login account. Id is Form.PublicId (not its internal, sequential Id) so hopping
/// between forms one isn't meant to see isn't possible by guessing. Id is required; UId is
/// required unless the form is anonymous.
/// </summary>
[AllowAnonymous]
public class FormularController(IFormManager formManager) : Controller
{
    public async Task<IActionResult> Index(Guid id, Guid? uId, CancellationToken cancellationToken)
    {
        var publicForm = await formManager.GetPublicFormAsync(id, uId, cancellationToken);
        return View(publicForm.ToPublicFormAccessViewModel(uId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id, Guid? uId, List<FormAnswerInputViewModel> answers, CancellationToken cancellationToken)
    {
        var input = answers.ToFormAnswerInputDtos();
        var result = await formManager.SubmitPublicAsync(id, uId, input, cancellationToken);

        if (result.Status == PublicFormStatus.Ok && result.Success)
        {
            return View("Index", new PublicFormAccessViewModel
            {
                Status = PublicFormStatus.Ok,
                Submitted = true,
                Form = new FormFillViewModel { Title = result.FormTitle ?? string.Empty }
            });
        }

        // The gate can fail here even though the GET succeeded (e.g. someone else used the
        // same link in between, or the form was closed) — re-resolve for a fresh, accurate view.
        var model = (await formManager.GetPublicFormAsync(id, uId, cancellationToken)).ToPublicFormAccessViewModel(uId);
        if (result.Status != PublicFormStatus.Ok)
            return View("Index", model);

        model.Form?.ApplySubmittedAnswers(input);
        model.FieldErrors = result.FieldErrors;
        return View("Index", model);
    }
}
