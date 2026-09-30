using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using web.Constants;
using web.Infrastructure;
using web.Infrastructure.Labels;
using web.Repositories.LabelCollections.Dtos;

namespace web.Repositories.LabelCollections
{
    /// <summary>
    /// Prints designed labels (Værktøjer → Labels) on A4 label sheets with QuestPDF. The sheet is split
    /// exactly like LabelSheetPdf (no page margin, across × down equal cells), and each label is drawn
    /// from its elements at their positions in % of the label — the same layout as the designer
    /// (wwwroot/js/label-design.js): Lato text with line height 1.2 and a small padding, shrunk if it
    /// doesn't fit its box; images and QR codes as large as fits, centred in their box.
    /// </summary>
    internal static class LabelDesignPdf
    {
        private const float PointsPerMm = 72f / 25.4f;

        /// <summary>Keep in sync with .lbl-text in wwwroot/css/site.css (line-height and padding in % of the label height).</summary>
        private const float LineHeight = 1.2f;
        private const float TextPaddingVertical = 0.01f;
        private const float TextPaddingHorizontal = 0.02f;

        /// <summary>A label and which copy of it (0-based) — the copy drives the løbenummer.</summary>
        private sealed record Copy(LabelDesignDto Label, int Index);

        /// <summary>
        /// The labels, each repeated Quantity times in order, as a PDF titled <paramref name="title"/>.
        /// <paramref name="images"/> maps a media id to its file on disk; elements with other ids are skipped.
        /// </summary>
        public static byte[] Build(string title, LabelCollectionDesignDto design, IReadOnlyDictionary<Guid, string> images, bool cutMarks)
        {
            // Exactly 210 × 297 mm (QuestPDF's PageSizes.A4 is rounded to whole points).
            var pageWidth = (design.Landscape ? 297f : 210f) * PointsPerMm;
            var pageHeight = (design.Landscape ? 210f : 297f) * PointsPerMm;
            // Rounded down to 1/1000 pt so the rows can never add up to more than the page (float rounding).
            var labelHeight = MathF.Floor(pageHeight / design.Down * 1000f) / 1000f;
            var labelWidth = pageWidth / design.Across;
            var perSheet = design.Across * design.Down;
            var cutMarksSvg = cutMarks ? LabelSheetPdf.CutMarksSvg(pageWidth, pageHeight, labelWidth, labelHeight, design.Across, design.Down) : null;

            var copies = design.Labels.SelectMany(l => Enumerable.Range(0, l.Quantity).Select(i => new Copy(l, i))).ToList();
            var resources = new Resources(images);

            return Document.Create(document =>
            {
                for (var first = 0; first < copies.Count; first += perSheet)
                {
                    var sheet = copies.Skip(first).Take(perSheet).ToList();
                    document.Page(page =>
                    {
                        page.Size(pageWidth, pageHeight, Unit.Point);
                        page.Margin(0);
                        if (cutMarksSvg is not null)
                            page.Foreground().Svg(cutMarksSvg);
                        page.Content().Column(column =>
                        {
                            for (var start = 0; start < sheet.Count; start += design.Across)
                            {
                                var rowStart = start;
                                column.Item().Height(labelHeight).Row(cells =>
                                {
                                    for (var col = 0; col < design.Across; col++)
                                    {
                                        var cell = cells.RelativeItem();
                                        if (rowStart + col < sheet.Count)
                                            Label(cell, sheet[rowStart + col], labelWidth, labelHeight, resources);
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

        private static void Label(IContainer cell, Copy copy, float width, float height, Resources resources)
        {
            cell.Layers(layers =>
            {
                var background = layers.PrimaryLayer().Extend();
                if (!string.Equals(copy.Label.Background, LabelDesignRules.DefaultBackground, StringComparison.OrdinalIgnoreCase))
                    background.Background(Color.FromHex(copy.Label.Background));

                // List order is the stacking order: later layers are drawn on top.
                foreach (var element in copy.Label.Elements)
                {
                    var box = layers.Layer()
                        .TranslateX((float)(element.X / 100 * width))
                        .TranslateY((float)(element.Y / 100 * height))
                        .Unconstrained()
                        .Width((float)(element.W / 100 * width))
                        .Height((float)(element.H / 100 * height));
                    Element(box, element, copy.Index, height, resources);
                }
            });
        }

        private static void Element(IContainer box, LabelElementDto element, int copyIndex, float labelHeight, Resources resources)
        {
            switch (element.Type)
            {
                case LabelDesignRules.ElementText:
                    Text(box, element, element.Text ?? string.Empty, labelHeight);
                    break;

                case LabelDesignRules.ElementSerial:
                    Text(box, element, SerialText(element, copyIndex), labelHeight);
                    break;

                case LabelDesignRules.ElementImage:
                    var image = element.MediaId is Guid mediaId ? resources.Image(mediaId) : null;
                    if (image is not null)
                        box.AlignCenter().AlignMiddle().Image(image).FitArea();
                    break;

                case LabelDesignRules.ElementQr:
                    if (!string.IsNullOrEmpty(element.Text))
                        box.AlignCenter().AlignMiddle().Svg(resources.Qr(element.Text, element.Color ?? "#000000")).FitArea();
                    break;
            }
        }

        /// <summary>Prefix + (Start + copy) padded to Digits + Suffix — keep in sync with serialText in label-design.js.</summary>
        public static string SerialText(LabelElementDto element, int copyIndex)
        {
            var number = ((element.Start ?? 1) + copyIndex).ToString(System.Globalization.CultureInfo.InvariantCulture)
                .PadLeft(element.Digits ?? 0, '0');
            return $"{element.Prefix}{number}{element.Suffix}";
        }

        private static void Text(IContainer box, LabelElementDto element, string text, float labelHeight)
        {
            if (!string.IsNullOrEmpty(element.Background))
                box = box.Background(Color.FromHex(element.Background));
            if (text.Length == 0)
                return;

            box = box.PaddingVertical(labelHeight * TextPaddingVertical).PaddingHorizontal(labelHeight * TextPaddingHorizontal)
                // Shrinks the text (only) when it doesn't fit its box — the designer does the same.
                .ScaleToFit();
            box = element.VAlign switch
            {
                "top" => box.AlignTop(),
                "bottom" => box.AlignBottom(),
                _ => box.AlignMiddle()
            };
            box = element.Align switch
            {
                "left" => box.AlignLeft(),
                "right" => box.AlignRight(),
                _ => box.AlignCenter()
            };

            box.Text(t =>
            {
                switch (element.Align)
                {
                    case "left": t.AlignLeft(); break;
                    case "right": t.AlignRight(); break;
                    default: t.AlignCenter(); break;
                }

                var span = t.Span(text)
                    .FontSize(Math.Max(1f, (float)((element.FontSize ?? LabelDesignRules.DefaultFontSize) / 100 * labelHeight)))
                    .FontColor(Color.FromHex(element.Color ?? LabelDesignRules.DefaultTextColor))
                    .LineHeight(LineHeight);
                if (element.Bold == true)
                    span.Bold();
                if (element.Italic == true)
                    span.Italic();
            });
        }

        /// <summary>Images and QR codes are loaded once and reused on every copy.</summary>
        private sealed class Resources
        {
            private readonly IReadOnlyDictionary<Guid, string> _imagePaths;
            private readonly Dictionary<Guid, Image?> _images = new();
            private readonly Dictionary<(string, string), SvgImage> _qrCodes = new();

            public Resources(IReadOnlyDictionary<Guid, string> imagePaths) => _imagePaths = imagePaths;

            public Image? Image(Guid mediaId)
            {
                if (!_images.TryGetValue(mediaId, out var image))
                {
                    image = _imagePaths.TryGetValue(mediaId, out var path) && File.Exists(path) ? QuestPDF.Infrastructure.Image.FromFile(path) : null;
                    _images[mediaId] = image;
                }
                return image;
            }

            public SvgImage Qr(string text, string color)
            {
                if (!_qrCodes.TryGetValue((text, color), out var svg))
                {
                    svg = SvgImage.FromText(QrCodeSvg.Create(text, color));
                    _qrCodes[(text, color)] = svg;
                }
                return svg;
            }
        }
    }
}
