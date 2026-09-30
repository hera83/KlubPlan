using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using web.Constants;
using web.ViewModels;

namespace web.Infrastructure.Labels
{
    /// <summary>
    /// Lays labels out on A4 label sheets (QuestPDF). The page has no margin and is split into exactly
    /// across × down equal cells, so the grid reaches the paper's edge and matches pre-cut sticker
    /// sheets. Labels fill each sheet from the top left, row by row; every text is centred and sized
    /// to its label: as large as fits, smaller for long texts and small labels. Optional cut marks let
    /// uncut full-sheet paper be cut to the same grid with a paper cutter.
    /// Shared by every "Print labels" in the app (Labels under Værktøjer, labels on work list lines).
    /// </summary>
    public static class LabelSheetPdf
    {
        /// <summary>Space between the text and the label's edge, as a share of the label's shortest side — keeps text off the cut line.</summary>
        private const float PaddingShare = 0.07f;

        /// <summary>Height of one line of text relative to the font size (Lato).</summary>
        private const float LineBox = 1.2f;

        /// <summary>Safety margin on the estimated word width, so a word is never split across lines.</summary>
        private const float WordWidthMargin = 1.08f;

        /// <summary>A short single line never gets a font size above this share of the label height, so it doesn't look oversized.</summary>
        private const float MaxFontShareOfHeight = 0.45f;

        /// <summary>Only a guard against zero — a long word on a tiny label is shrunk rather than split.</summary>
        private const float MinFontSize = 1f;

        private const float PointsPerMm = 72f / 25.4f;

        /// <summary>Cut marks: length of each arm from a label corner along the cut line.</summary>
        private const float CutMarkArmMm = 3f;

        /// <summary>Cut marks at the paper's edge are longer, so they still show past the printer's unprintable margin (≈ 3–5 mm).</summary>
        private const float CutMarkEdgeMm = 9f;

        /// <summary>A cut mark never covers more than this share of the label side it runs along, so it stays a mark, not a line.</summary>
        private const float CutMarkMaxShare = 0.4f;

        private const float CutMarkStrokeWidth = 0.4f;
        private const string CutMarkColor = "#8a8a8a";

        /// <summary>
        /// The labels, each repeated Quantity times in the given order, as a PDF titled "Labels - {title}".
        /// Fails (with a Danish message for the toast) when there's nothing to print, too much to print
        /// (<paramref name="tooManyHint"/> is appended to that message), or QuestPDF fails.
        /// </summary>
        public static LabelPdfResult Create(string title, IReadOnlyList<LabelRow> labels, LabelSheetViewModel sheet, ILogger logger, string emptyMessage, string? tooManyHint = null)
        {
            var copies = labels.Sum(l => l.Quantity);
            if (copies == 0)
                return LabelPdfResult.Fail(emptyMessage);
            if (copies > LabelRules.MaxLabelsPerPdf)
                return LabelPdfResult.Fail($"Der er {copies} labels, men én PDF kan højst have {LabelRules.MaxLabelsPerPdf}." + (tooManyHint is null ? "" : $" {tooManyHint}"));

            var texts = labels.SelectMany(l => Enumerable.Repeat(l.Text, l.Quantity)).ToList();
            var pdfTitle = $"Labels - {title}";
            byte[] content;
            try
            {
                content = Build(pdfTitle, texts, sheet.Across, sheet.Down, sheet.Landscape, sheet.CutMarks);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Labels {Title} could not be generated ({Across}x{Down})", pdfTitle, sheet.Across, sheet.Down);
                return LabelPdfResult.Fail("PDF'en med labels kunne ikke dannes.");
            }

            return new LabelPdfResult
            {
                Success = true,
                Content = content,
                Title = pdfTitle,
                FileName = FileNames.Sanitize(pdfTitle, "labels") + ".pdf"
            };
        }

        public static byte[] Build(string title, IReadOnlyList<string> labels, int across, int down, bool landscape, bool cutMarks = false)
        {
            // Exactly 210 × 297 mm (QuestPDF's PageSizes.A4 is rounded to whole points).
            var pageWidth = (landscape ? 297f : 210f) * PointsPerMm;
            var pageHeight = (landscape ? 210f : 297f) * PointsPerMm;
            // Rounded down to 1/1000 pt so the rows can never add up to more than the page (float rounding).
            var labelHeight = MathF.Floor(pageHeight / down * 1000f) / 1000f;
            var labelWidth = pageWidth / across;
            var padding = Math.Min(labelWidth, labelHeight) * PaddingShare;
            var innerWidth = labelWidth - 2 * padding;
            var innerHeight = labelHeight - 2 * padding;
            var perSheet = across * down;
            var cutMarksSvg = cutMarks ? CutMarksSvg(pageWidth, pageHeight, labelWidth, labelHeight, across, down) : null;

            return Document.Create(document =>
            {
                for (var first = 0; first < labels.Count; first += perSheet)
                {
                    var sheet = labels.Skip(first).Take(perSheet).ToList();
                    document.Page(page =>
                    {
                        page.Size(pageWidth, pageHeight, Unit.Point);
                        page.Margin(0);
                        if (cutMarksSvg is not null)
                            page.Foreground().Svg(cutMarksSvg);
                        page.Content().Column(column =>
                        {
                            for (var start = 0; start < sheet.Count; start += across)
                            {
                                var rowStart = start;
                                column.Item().Height(labelHeight).Row(cells =>
                                {
                                    for (var col = 0; col < across; col++)
                                    {
                                        var cell = cells.RelativeItem();
                                        if (rowStart + col < sheet.Count)
                                            Label(cell, sheet[rowStart + col], padding, innerWidth, innerHeight);
                                    }
                                });
                            }
                        });
                    });
                }
            })
            .WithMetadata(new DocumentMetadata { Title = title })
            .GeneratePdf();
        }

        /// <summary>
        /// Short marks on every cut line (the lines between labels — the paper's edge needs no cut), at
        /// both ends of every label side: at each label corner the mark runs a few mm each way along the
        /// cut line. They sit exactly on the cut, inside the label padding, so they never touch the text
        /// and are cut away. Every strip cut off the sheet still carries the marks for its next cuts.
        /// </summary>
        internal static string CutMarksSvg(float pageWidth, float pageHeight, float labelWidth, float labelHeight, int across, int down)
        {
            var arm = CutMarkArmMm * PointsPerMm;
            var edge = CutMarkEdgeMm * PointsPerMm;
            var lines = new List<string>();

            // Vertical cut lines between columns: marks at the top/bottom edge and at each row boundary.
            for (var col = 1; col < across; col++)
            {
                var x = col * labelWidth;
                for (var row = 0; row <= down; row++)
                {
                    var y = row == down ? pageHeight : row * labelHeight;
                    var length = Math.Min(row == 0 || row == down ? edge : arm, labelHeight * CutMarkMaxShare);
                    lines.Add(SvgLine(x, Math.Max(0, y - length), x, Math.Min(pageHeight, y + length)));
                }
            }

            // Horizontal cut lines between rows: marks at the left/right edge and at each column boundary.
            for (var row = 1; row < down; row++)
            {
                var y = row * labelHeight;
                for (var col = 0; col <= across; col++)
                {
                    var x = col * labelWidth;
                    var length = Math.Min(col == 0 || col == across ? edge : arm, labelWidth * CutMarkMaxShare);
                    lines.Add(SvgLine(Math.Max(0, x - length), y, Math.Min(pageWidth, x + length), y));
                }
            }

            return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{F(pageWidth)}\" height=\"{F(pageHeight)}\" viewBox=\"0 0 {F(pageWidth)} {F(pageHeight)}\">"
                + $"<g stroke=\"{CutMarkColor}\" stroke-width=\"{F(CutMarkStrokeWidth)}\" fill=\"none\">{string.Concat(lines)}</g></svg>";
        }

        private static string SvgLine(float x1, float y1, float x2, float y2)
            => $"<line x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\"/>";

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static void Label(IContainer cell, string text, float padding, float innerWidth, float innerHeight)
        {
            // Start at the largest sensible size; ScaleToFit then shrinks it until the wrapped text fits.
            cell.Padding(padding)
                .ScaleToFit()
                .AlignMiddle()
                .AlignCenter()
                .Text(text)
                .FontSize(StartFontSize(text, innerWidth, innerHeight))
                .AlignCenter();
        }

        /// <summary>
        /// Limited by the height (every text line must fit) and by the width (the longest word must fit
        /// on one line, so words aren't split). The label size drives both, so 2 labels across give much
        /// larger text than 4 across.
        /// </summary>
        private static float StartFontSize(string text, float innerWidth, float innerHeight)
        {
            var lineCount = text.Split('\n').Length;
            var widestWord = text.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(WordWidthEm)
                .DefaultIfEmpty(1f)
                .Max();

            var byHeight = Math.Min(innerHeight / (lineCount * LineBox), innerHeight * MaxFontShareOfHeight);
            var byWidth = innerWidth / (widestWord * WordWidthMargin);
            return Math.Max(MinFontSize, Math.Min(byHeight, byWidth));
        }

        /// <summary>Approximate width of a word in em, from Lato's glyph widths (lowercase ≈ 0.52, capitals ≈ 0.68).</summary>
        private static float WordWidthEm(string word) => word.Sum(c => c switch
        {
            'm' or 'w' => 0.86f,
            'M' or 'W' or 'Æ' or 'æ' => 1.05f,
            'i' or 'l' or 'j' or 't' or 'f' or 'r' or 'I' or '.' or ',' or ':' or ';' or '\'' or '!' or '|' => 0.34f,
            _ when char.IsUpper(c) => 0.7f,
            _ when char.IsDigit(c) => 0.58f,
            _ => 0.53f
        });
    }
}
