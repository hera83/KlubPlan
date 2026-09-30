namespace web.Infrastructure.Labels
{
    /// <summary>A generated label-sheet PDF (or why it couldn't be made) — see <see cref="LabelSheetPdf.Create"/>.</summary>
    public class LabelPdfResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = "labels.pdf";

        /// <summary>PDF title — the browser's PDF viewer shows it on the tab.</summary>
        public string Title { get; set; } = "Labels";

        public static LabelPdfResult Fail(string error) => new() { Success = false, ErrorMessage = error };
    }
}
