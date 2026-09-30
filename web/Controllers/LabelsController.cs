using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Infrastructure;
using web.Infrastructure.Labels;
using web.Repositories.LabelCollections.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    /// <summary>Værktøjer → Labels: label collections with create/edit/delete and "Print labels" (PDF).</summary>
    [Authorize]
    public class LabelsController : Controller
    {
        private readonly ILabelCollectionService _collectionService;

        public LabelsController(ILabelCollectionService collectionService)
        {
            _collectionService = collectionService;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var model = await _collectionService.GetCollectionsAsync(new LabelCollectionFilterViewModel(), ct);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CollectionsTable(LabelCollectionFilterViewModel filter, CancellationToken ct)
        {
            var model = await _collectionService.GetCollectionsAsync(filter, ct);
            return PartialView("_LabelCollectionsTableBody", model);
        }

        /// <summary>Name and labels for the "Rediger" modal.</summary>
        [HttpGet]
        public async Task<IActionResult> Collection(int id, CancellationToken ct)
        {
            var model = await _collectionService.GetForEditAsync(id, ct);
            return model is null ? this.ToastErrorJson("Label-samlingen blev ikke fundet.") : Json(new { success = true, model.Id, model.Name, model.Labels });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(LabelCollectionEditViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson(FirstModelError() ?? "Label-samlingen kunne ikke gemmes.");

            var result = await _collectionService.SaveAsync(model, ct);
            return result.Success ? this.ToastSuccessJson(result.Message!) : this.ToastErrorJson(result.ErrorMessage!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var deleted = await _collectionService.DeleteAsync(id, ct);
            return deleted
                ? this.ToastSuccessJson("Label-samlingen er slettet.")
                : this.ToastErrorJson("Label-samlingen blev ikke fundet.");
        }

        /// <summary>The collection's label sheets as a PDF, shown in the browser (new tab) so it can be printed from there.</summary>
        [HttpGet]
        public async Task<IActionResult> Pdf(int id, LabelSheetViewModel sheet, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                this.ToastError(FirstModelError() ?? "Labels kunne ikke printes.");
                return RedirectToAction(nameof(Index));
            }

            var pdf = await _collectionService.BuildPdfAsync(id, sheet, ct);
            if (pdf is null)
                return NotFound();
            if (!pdf.Success)
            {
                this.ToastWarning(pdf.ErrorMessage!);
                return RedirectToAction(nameof(Index));
            }

            return this.InlineLabelPdf(pdf);
        }

        private string? FirstModelError()
            => ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
    }
}
