using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace web.Infrastructure.Labels
{
    public static class LabelPdfControllerExtensions
    {
        /// <summary>The label PDF shown in the browser (new tab), so it can be printed from there.</summary>
        public static FileContentResult InlineLabelPdf(this Controller controller, LabelPdfResult pdf)
        {
            var disposition = new ContentDispositionHeaderValue("inline");
            disposition.SetHttpFileName(pdf.FileName);
            controller.Response.Headers.ContentDisposition = disposition.ToString();
            return controller.File(pdf.Content, "application/pdf");
        }
    }
}
