using System.Linq.Expressions;
using System.Text.Json;
using web.Constants;
using web.Data.Models;
using web.Repositories.InfoScreens.Dtos;

namespace web.Repositories.InfoScreens.Mappers;

public static class InfoScreenMapper
{
    /// <summary>camelCase both ways — the designer and the player read/write the same JSON.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static readonly Expression<Func<InfoScreen, InfoScreenListItemDto>> ToListItemDtoExpression = x => new InfoScreenListItemDto
    {
        Id = x.Id,
        PublicId = x.PublicId,
        Title = x.Title,
        IsActive = x.IsActive,
        SlideCount = x.Slides.Count,
        VisibleSlideCount = x.Slides.Count(s => !s.IsHidden),
        TotalSeconds = x.Slides.Where(s => !s.IsHidden).Sum(s => (int?)s.DurationSeconds) ?? 0,
        UpdatedAt = x.UpdatedAt
    };

    /// <summary>A new screen with one starter slide showing the title, so the designer opens on something to edit.</summary>
    public static InfoScreen ToEntity(this CreateInfoScreenDto dto, string title)
    {
        var screen = new InfoScreen
        {
            Title = title,
            IsActive = dto.IsActive,
            AspectRatio = InfoScreenRules.DefaultAspectRatio,
            Transition = InfoScreenRules.DefaultTransition
        };

        screen.Slides.Add(new InfoScreenSlide
        {
            Order = 0,
            DurationSeconds = InfoScreenRules.DefaultDurationSeconds,
            Background = InfoScreenRules.DefaultBackground,
            ElementsJson = JsonSerializer.Serialize(new[]
            {
                new InfoScreenElementDto
                {
                    Id = "e1", Type = InfoScreenRules.ElementText, X = 10, Y = 35, W = 80, H = 30,
                    Text = title, FontSize = 12, FontFamily = "sans", Bold = true, Italic = false,
                    Align = "center", VAlign = "middle", Color = "#ffffff", Background = ""
                }
            }, JsonOptions)
        });

        return screen;
    }

    public static InfoScreenDesignDto ToDesignDto(this InfoScreen screen) => new()
    {
        Title = screen.Title,
        IsActive = screen.IsActive,
        AspectRatio = screen.AspectRatio,
        Transition = screen.Transition,
        Slides = screen.Slides.OrderBy(s => s.Order).Select(ToDto).ToList()
    };

    public static InfoScreenSlideDto ToDto(this InfoScreenSlide slide) => new()
    {
        DurationSeconds = slide.DurationSeconds,
        Background = slide.Background,
        IsHidden = slide.IsHidden,
        Elements = DeserializeElements(slide.ElementsJson)
    };

    public static InfoScreenSlide ToEntity(this InfoScreenSlideDto slide, int order) => new()
    {
        Order = order,
        DurationSeconds = slide.DurationSeconds,
        Background = slide.Background,
        IsHidden = slide.IsHidden,
        ElementsJson = JsonSerializer.Serialize(slide.Elements, JsonOptions)
    };

    public static InfoScreenMediaDto ToDto(this InfoScreenMedia media) => new()
    {
        Id = media.PublicId,
        Name = media.FileMetadata.OriginalFileName,
        Kind = media.Kind,
        SizeBytes = media.FileMetadata.FileSizeBytes
    };

    private static List<InfoScreenElementDto> DeserializeElements(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<InfoScreenElementDto>>(json, JsonOptions) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }
}
