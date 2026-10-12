using System.Text.RegularExpressions;
using web.Constants;
using web.Repositories.InfoScreens.Dtos;

namespace web.Repositories.InfoScreens;

/// <summary>
/// Validates and normalizes a design from the designer before it is saved: limits from
/// InfoScreenRules, numbers clamped, colors/choices checked, and only the fields for each
/// element's type kept. Pure — no database or files.
/// </summary>
public static class InfoScreenDesignNormalizer
{
    private static readonly Regex HexColor = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);
    private static readonly Regex ElementId = new("^[A-Za-z0-9_-]{1,40}$", RegexOptions.Compiled);

    /// <summary>The normalized design, or an error message (in Danish, for the user) when a rule is broken.</summary>
    public static (InfoScreenDesignDto? Design, string? Error) Normalize(InfoScreenDesignDto design, IReadOnlySet<Guid> mediaIds)
    {
        var title = (design.Title ?? string.Empty).Trim();
        if (title.Length == 0)
            return (null, "Titel skal udfyldes.");
        if (title.Length > InfoScreenRules.MaxTitleLength)
            return (null, $"Titlen må højst være {InfoScreenRules.MaxTitleLength} tegn.");
        if (design.Slides.Count > InfoScreenRules.MaxSlides)
            return (null, $"En infoskærm kan højst have {InfoScreenRules.MaxSlides} sider.");

        var slides = new List<InfoScreenSlideDto>();
        for (var n = 0; n < design.Slides.Count; n++)
        {
            var input = design.Slides[n];
            if (input.Elements.Count > InfoScreenRules.MaxElementsPerSlide)
                return (null, $"Side {n + 1} har for mange elementer (højst {InfoScreenRules.MaxElementsPerSlide}).");

            var elements = new List<InfoScreenElementDto>();
            foreach (var element in input.Elements)
            {
                var normalized = NormalizeElement(element, mediaIds, out var error);
                if (error is not null)
                    return (null, $"Side {n + 1}: {error}");
                if (normalized is not null)
                    elements.Add(normalized);
            }

            slides.Add(new InfoScreenSlideDto
            {
                DurationSeconds = Math.Clamp(input.DurationSeconds, InfoScreenRules.MinDurationSeconds, InfoScreenRules.MaxDurationSeconds),
                Background = ColorOr(input.Background, InfoScreenRules.DefaultBackground),
                IsHidden = input.IsHidden,
                Elements = elements
            });
        }

        return (new InfoScreenDesignDto
        {
            Title = title,
            IsActive = design.IsActive,
            AspectRatio = InfoScreenRules.AspectRatios.Any(a => a.Key == design.AspectRatio) ? design.AspectRatio : InfoScreenRules.DefaultAspectRatio,
            Transition = InfoScreenRules.Transitions.Any(t => t.Key == design.Transition) ? design.Transition : InfoScreenRules.DefaultTransition,
            Slides = slides
        }, null);
    }

    /// <summary>Only the fields for the element's type are kept; numbers are clamped and colors/choices checked. Null = drop it.</summary>
    private static InfoScreenElementDto? NormalizeElement(InfoScreenElementDto e, IReadOnlySet<Guid> mediaIds, out string? error)
    {
        error = null;
        if (!InfoScreenRules.ElementTypes.Contains(e.Type))
            return null;

        var result = new InfoScreenElementDto
        {
            Id = e.Id is not null && ElementId.IsMatch(e.Id) ? e.Id : Guid.NewGuid().ToString("N")[..12],
            Type = e.Type,
            X = Round(Math.Clamp(e.X, -50, 100)),
            Y = Round(Math.Clamp(e.Y, -50, 100)),
            W = Round(Math.Clamp(e.W, 0.5, 200)),
            H = Round(Math.Clamp(e.H, 0.5, 200))
        };

        switch (e.Type)
        {
            case InfoScreenRules.ElementText:
            case InfoScreenRules.ElementClock:
                if (e.Type == InfoScreenRules.ElementText)
                {
                    var text = (e.Text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
                    if (text.Length > InfoScreenRules.MaxTextLength)
                    {
                        error = $"En tekst må højst være {InfoScreenRules.MaxTextLength} tegn.";
                        return null;
                    }
                    result.Text = text;
                }
                else
                {
                    result.Format = Choice(e.Format, InfoScreenRules.ClockFormats, "time");
                }
                result.FontSize = Round(Math.Clamp(e.FontSize ?? 8, InfoScreenRules.MinFontSize, InfoScreenRules.MaxFontSize));
                result.FontFamily = Choice(e.FontFamily, InfoScreenRules.FontFamilies, "sans");
                result.Bold = e.Bold ?? false;
                result.Italic = e.Italic ?? false;
                result.Align = Choice(e.Align, InfoScreenRules.Aligns, "center");
                result.VAlign = Choice(e.VAlign, InfoScreenRules.VAligns, "middle");
                result.Color = ColorOr(e.Color, "#ffffff");
                result.Background = ColorOr(e.Background, string.Empty);
                break;

            case InfoScreenRules.ElementImage:
            case InfoScreenRules.ElementVideo:
                // A file removed from the library leaves an empty box rather than a broken element.
                result.MediaId = e.MediaId is Guid mediaId && mediaIds.Contains(mediaId) ? mediaId : null;
                result.Fit = Choice(e.Fit, InfoScreenRules.Fits, "contain");
                break;

            case InfoScreenRules.ElementQr:
                var qrText = (e.Text ?? string.Empty).Trim();
                if (qrText.Length > InfoScreenRules.MaxQrTextLength)
                {
                    error = $"Teksten i en QR-kode må højst være {InfoScreenRules.MaxQrTextLength} tegn.";
                    return null;
                }
                result.Text = qrText;
                result.Color = ColorOr(e.Color, "#000000");
                break;
        }
        return result;
    }

    private static double Round(double value) => Math.Round(value, 2);

    private static string Choice(string? value, IReadOnlySet<string> allowed, string fallback)
        => value is not null && allowed.Contains(value) ? value : fallback;

    private static string ColorOr(string? value, string fallback)
        => value is not null && HexColor.IsMatch(value) ? value.ToLowerInvariant() : fallback;
}
