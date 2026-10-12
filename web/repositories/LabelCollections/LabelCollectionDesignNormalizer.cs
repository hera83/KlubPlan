using System.Text.RegularExpressions;
using web.Constants;
using web.Repositories.LabelCollections.Dtos;

namespace web.Repositories.LabelCollections;

/// <summary>
/// Validates and normalizes a design from the label designer before it is saved: limits from
/// LabelRules/LabelDesignRules, numbers clamped (boxes kept inside the label — on a sheet anything
/// outside would print on the neighbour), colors/choices checked, and only the fields for each
/// element's type kept. Pure — no database or files.
/// </summary>
public static class LabelCollectionDesignNormalizer
{
    private static readonly Regex HexColor = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);
    private static readonly Regex ElementId = new("^[A-Za-z0-9_-]{1,40}$", RegexOptions.Compiled);

    /// <summary>The normalized design, or an error message (in Danish, for the user) when a rule is broken.</summary>
    public static (LabelCollectionDesignDto? Design, string? Error) Normalize(LabelCollectionDesignDto design, IReadOnlySet<Guid> mediaIds)
    {
        var name = (design.Name ?? string.Empty).Trim();
        if (name.Length == 0)
            return (null, "Navn skal udfyldes.");
        if (name.Length > LabelRules.MaxCollectionNameLength)
            return (null, $"Navnet må højst være {LabelRules.MaxCollectionNameLength} tegn.");
        if (design.Across is < 1 or > LabelRules.MaxAcross)
            return (null, $"Labels i bredden skal være mellem 1 og {LabelRules.MaxAcross}.");
        if (design.Down is < 1 or > LabelRules.MaxDown)
            return (null, $"Labels i højden skal være mellem 1 og {LabelRules.MaxDown}.");
        if (design.Labels.Count > LabelRules.MaxLabelsPerCollection)
            return (null, $"En samling kan højst have {LabelRules.MaxLabelsPerCollection} forskellige labels.");

        var labels = new List<LabelDesignDto>();
        for (var n = 0; n < design.Labels.Count; n++)
        {
            var input = design.Labels[n];
            if (input.Quantity is < 1 or > LabelRules.MaxQuantity)
                return (null, $"Label {n + 1}: antal skal være mellem 1 og {LabelRules.MaxQuantity}.");
            if (input.Elements.Count > LabelDesignRules.MaxElementsPerLabel)
                return (null, $"Label {n + 1} har for mange elementer (højst {LabelDesignRules.MaxElementsPerLabel}).");

            var elements = new List<LabelElementDto>();
            foreach (var element in input.Elements)
            {
                var normalized = NormalizeElement(element, mediaIds, out var error);
                if (error is not null)
                    return (null, $"Label {n + 1}: {error}");
                if (normalized is not null)
                    elements.Add(normalized);
            }

            labels.Add(new LabelDesignDto
            {
                Quantity = input.Quantity,
                Background = ColorOr(input.Background, LabelDesignRules.DefaultBackground),
                Elements = elements
            });
        }

        return (new LabelCollectionDesignDto
        {
            Name = name,
            Across = design.Across,
            Down = design.Down,
            Landscape = design.Landscape,
            Labels = labels
        }, null);
    }

    /// <summary>Only the fields for the element's type are kept; numbers are clamped and colors/choices checked. Null = drop it.</summary>
    private static LabelElementDto? NormalizeElement(LabelElementDto e, IReadOnlySet<Guid> mediaIds, out string? error)
    {
        error = null;
        if (!LabelDesignRules.ElementTypes.Contains(e.Type))
            return null;

        var w = Round(Math.Clamp(e.W, 0.5, 100));
        var h = Round(Math.Clamp(e.H, 0.5, 100));
        var result = new LabelElementDto
        {
            Id = e.Id is not null && ElementId.IsMatch(e.Id) ? e.Id : Guid.NewGuid().ToString("N")[..12],
            Type = e.Type,
            X = Round(Math.Clamp(e.X, 0, 100 - w)),
            Y = Round(Math.Clamp(e.Y, 0, 100 - h)),
            W = w,
            H = h
        };

        switch (e.Type)
        {
            case LabelDesignRules.ElementText:
            case LabelDesignRules.ElementSerial:
                if (e.Type == LabelDesignRules.ElementText)
                {
                    var text = (e.Text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
                    if (text.Length > LabelDesignRules.MaxTextLength)
                    {
                        error = $"En tekst må højst være {LabelDesignRules.MaxTextLength} tegn.";
                        return null;
                    }
                    result.Text = text;
                }
                else
                {
                    result.Start = Math.Clamp(e.Start ?? 1, 0, LabelDesignRules.MaxSerialStart);
                    result.Digits = Math.Clamp(e.Digits ?? 0, 0, LabelDesignRules.MaxSerialDigits);
                    result.Prefix = Affix(e.Prefix);
                    result.Suffix = Affix(e.Suffix);
                }
                result.FontSize = Round(Math.Clamp(e.FontSize ?? LabelDesignRules.DefaultFontSize, LabelDesignRules.MinFontSize, LabelDesignRules.MaxFontSize));
                result.Bold = e.Bold ?? false;
                result.Italic = e.Italic ?? false;
                result.Align = Choice(e.Align, LabelDesignRules.Aligns, "center");
                result.VAlign = Choice(e.VAlign, LabelDesignRules.VAligns, "middle");
                result.Color = ColorOr(e.Color, LabelDesignRules.DefaultTextColor);
                result.Background = ColorOr(e.Background, string.Empty);
                break;

            case LabelDesignRules.ElementImage:
                // An image removed from the library leaves an empty box rather than a broken element.
                result.MediaId = e.MediaId is Guid mediaId && mediaIds.Contains(mediaId) ? mediaId : null;
                break;

            case LabelDesignRules.ElementQr:
                var qrText = (e.Text ?? string.Empty).Trim();
                if (qrText.Length > LabelDesignRules.MaxQrTextLength)
                {
                    error = $"Teksten i en QR-kode må højst være {LabelDesignRules.MaxQrTextLength} tegn.";
                    return null;
                }
                result.Text = qrText;
                result.Color = ColorOr(e.Color, "#000000");
                break;
        }
        return result;
    }

    private static double Round(double value) => Math.Round(value, 2);

    private static string Affix(string? value)
    {
        var text = (value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", " ");
        return text.Length > LabelDesignRules.MaxSerialAffixLength ? text[..LabelDesignRules.MaxSerialAffixLength] : text;
    }

    private static string Choice(string? value, IReadOnlySet<string> allowed, string fallback)
        => value is not null && allowed.Contains(value) ? value : fallback;

    private static string ColorOr(string? value, string fallback)
        => value is not null && HexColor.IsMatch(value) ? value.ToLowerInvariant() : fallback;
}
