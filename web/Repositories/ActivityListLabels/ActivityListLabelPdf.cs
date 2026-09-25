using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace web.Repositories.ActivityListLabels
{
    /// <summary>
    /// Lays labels out on A4 label sheets (QuestPDF). The page has no margin and is split into exactly
    /// across × down equal cells, so the grid reaches the paper's edge and matches pre-cut sticker
    /// sheets. Labels fill each sheet from the top left, row by row; every text is centred and sized
    /// to its label: as large as fits, smaller for long texts and small labels.
    /// </summary>
    public static class ActivityListLabelPdf
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

        public static byte[] Build(string title, IReadOnlyList<string> labels, int across, int down, bool landscape)
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

            return Document.Create(document =>
            {
                for (var first = 0; first < labels.Count; first += perSheet)
                {
                    var sheet = labels.Skip(first).Take(perSheet).ToList();
                    document.Page(page =>
                    {
                        page.Size(pageWidth, pageHeight, Unit.Point);
                        page.Margin(0);
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
