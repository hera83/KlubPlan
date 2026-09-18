using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Repositories.Forms.Dtos;
using web.Repositories.Forms.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    /// <summary>
    /// Public, unauthenticated form links: /Formular?Id={form.PublicId}&amp;UId={person.PublicId}.
    /// Lets a form be sent to a group of People (Data/Entities/Person) who don't have — and don't
    /// need — a login account. Id is Form.PublicId (not its internal, sequential Id) so hopping
    /// between forms one isn't meant to see isn't possible by guessing. Id is required; UId is
    /// required unless the form is anonymous.
    /// </summary>
    [AllowAnonymous]
    public class FormularController : Controller
    {
        private readonly IFormService _formService;

        public FormularController(IFormService formService)
        {
            _formService = formService;
        }

        public async Task<IActionResult> Index(Guid id, Guid? uId, CancellationToken ct)
        {
            var model = await _formService.GetPublicFormAsync(id, uId, ct);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Guid id, Guid? uId, List<FormAnswerInputViewModel> answers, CancellationToken ct)
        {
            var dto = new SubmitPublicFormRequestDto
            {
                FormPublicId = id,
                PersonPublicId = uId,
                Answers = (answers ?? new List<FormAnswerInputViewModel>()).Select(a => new SubmitFormAnswerDto
                {
                    FormFieldId = a.FormFieldId,
                    Value = a.Value,
                    Values = a.Values
                }).ToList()
            };

            var result = await _formService.SubmitPublicFormAsync(dto, ct);

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
            if (result.Status != PublicFormStatus.Ok)
            {
                var gate = await _formService.GetPublicFormAsync(id, uId, ct);
                return View("Index", gate);
            }

            var model = await _formService.GetPublicFormAsync(id, uId, ct);
            if (model.Form is not null)
            {
                var answerLookup = dto.Answers.ToDictionary(a => a.FormFieldId);
                foreach (var field in model.Form.Fields)
                {
                    if (!answerLookup.TryGetValue(field.FormFieldId, out var answer)) continue;
                    field.SubmittedValue = answer.Value;
                    field.SubmittedValues = answer.Values;
                }
            }

            model.FieldErrors = result.FieldErrors;
            return View("Index", model);
        }
    }
}
