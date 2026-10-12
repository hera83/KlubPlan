using web.Repositories.InfoScreens.Dtos;

namespace web.ViewModels.Mappers;

/// <summary>Maps between the InfoScreens view models and the InfoScreens repository's Dtos.</summary>
public static class InfoScreensMapper
{
    public static SearchInfoScreensDto ToSearchInfoScreensDto(this InfoScreenFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        Status = filter.Status,
        Page = filter.Page,
        PageSize = filter.PageSize
    };

    public static InfoScreenFilterViewModel ToInfoScreenFilterViewModel(this InfoScreenPageDto page, InfoScreenFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        Status = filter.Status,
        Page = page.Page,
        PageSize = page.PageSize,
        TotalCount = page.TotalCount,
        Screens = page.Items.Select(s => new InfoScreenListItemViewModel
        {
            Id = s.Id,
            PublicId = s.PublicId,
            Title = s.Title,
            IsActive = s.IsActive,
            SlideCount = s.SlideCount,
            VisibleSlideCount = s.VisibleSlideCount,
            TotalSeconds = s.TotalSeconds,
            ChangedAtUtc = s.UpdatedAt
        }).ToList()
    };

    public static CreateInfoScreenDto ToCreateInfoScreenDto(this InfoScreenCreateViewModel model) => new()
    {
        Title = model.Title,
        IsActive = model.IsActive
    };

    public static InfoScreenDesignerViewModel ToInfoScreenDesignerViewModel(this InfoScreenDesignerDto designer) => new()
    {
        Id = designer.Id,
        PublicId = designer.PublicId,
        Design = designer.Design,
        Media = designer.Media
    };
}
