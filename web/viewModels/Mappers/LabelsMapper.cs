using web.Repositories.LabelCollections.Dtos;

namespace web.ViewModels.Mappers;

/// <summary>Maps between the Labels (Værktøjer → Labels) view models and the LabelCollections repository's Dtos.</summary>
public static class LabelsMapper
{
    public static SearchLabelCollectionsDto ToSearchLabelCollectionsDto(this LabelCollectionFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        Page = filter.Page,
        PageSize = filter.PageSize
    };

    public static LabelCollectionFilterViewModel ToLabelCollectionFilterViewModel(this LabelCollectionPageDto page, LabelCollectionFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        Page = page.Page,
        PageSize = page.PageSize,
        TotalCount = page.TotalCount,
        Collections = page.Items.Select(c => new LabelCollectionListItemViewModel
        {
            Id = c.Id,
            Name = c.Name,
            LabelCount = c.LabelCount,
            Copies = c.Copies,
            Across = c.Across,
            Down = c.Down,
            Landscape = c.Landscape,
            ChangedAtUtc = c.UpdatedAt
        }).ToList()
    };

    public static CreateLabelCollectionDto ToCreateLabelCollectionDto(this LabelCollectionCreateViewModel model) => new()
    {
        Name = model.Name,
        Across = model.Across,
        Down = model.Down,
        Landscape = model.Landscape
    };

    public static LabelDesignerViewModel ToLabelDesignerViewModel(this LabelCollectionDesignerDto designer) => new()
    {
        Id = designer.Id,
        Design = designer.Design,
        Media = designer.Media
    };
}
