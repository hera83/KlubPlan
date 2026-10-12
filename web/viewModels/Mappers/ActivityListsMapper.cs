using web.Extensions;
using web.Infrastructure.Labels;
using web.Managers.ActivityLists.Dtos;
using web.Repositories.ActivityLists.Dtos;

namespace web.ViewModels.Mappers;

public static class ActivityListsMapper
{
    /// <summary>"Ændret af X · 22. sep. 2026 14:30" for a line's history hint (null when never changed).</summary>
    public static string? UpdatedText(string? userName, DateTime? updatedAtUtc)
    {
        if (!updatedAtUtc.HasValue)
            return null;
        var when = updatedAtUtc.Value.ToLocalTime().ToDanishDateTime();
        return string.IsNullOrEmpty(userName) ? $"Ændret {when}" : $"Ændret af {userName} · {when}";
    }

    public static List<ActivityListSummaryViewModel> ToActivityListSummaryViewModels(this List<ActivityListSummaryDto> lists) =>
        lists.Select(l => new ActivityListSummaryViewModel
        {
            Id = l.Id,
            Title = l.Title,
            Description = l.Description,
            SourceFileName = l.SourceFileName,
            ItemCount = l.ItemCount,
            MineCount = l.MineCount,
            UnassignedCount = l.UnassignedCount,
            Statuses = l.Statuses.Select(ToViewModel).ToList(),
            CreatedAtUtc = l.CreatedAt,
            CreatedByName = l.CreatedByName
        }).ToList();

    public static ActivityListDetailsViewModel ToActivityListDetailsViewModel(this ActivityListDto list)
    {
        var schema = list.Schema.ToViewModel();
        return new ActivityListDetailsViewModel
        {
            Id = list.Id,
            ActivityId = list.ActivityId,
            ActivityTitle = list.ActivityTitle,
            Title = list.Title,
            Description = list.Description,
            SourceFileName = list.SourceFileName,
            CreatedAtUtc = list.CreatedAt,
            TotalCount = list.TotalCount,
            UnassignedCount = list.UnassignedCount,
            CurrentUserIsMember = list.CurrentUserIsMember,
            Schema = schema,
            Items = FillItems(new ActivityListItemsViewModel(), list.Items, schema)
        };
    }

    public static ActivityListItemsViewModel ToActivityListItemsViewModel(this ActivityListItemPageDto page) =>
        FillItems(new ActivityListItemsViewModel(), page, page.Schema.ToViewModel());

    public static PublicWorkListItemsViewModel ToPublicWorkListItemsViewModel(this ActivityListItemPageDto page) =>
        FillPublicItems(page, page.Schema.ToViewModel());

    public static PublicWorkListViewModel ToPublicWorkListViewModel(this ActivityListPublicDto list)
    {
        if (!list.Found)
        {
            return new PublicWorkListViewModel { Found = false };
        }

        var schema = list.Schema.ToViewModel();
        return new PublicWorkListViewModel
        {
            Found = true,
            ListId = list.ListId,
            MemberPublicId = list.MemberPublicId,
            MemberName = list.MemberName,
            ActivityTitle = list.ActivityTitle,
            Title = list.Title,
            Description = list.Description,
            Schema = schema,
            Items = FillPublicItems(list.Items, schema)
        };
    }

    public static SearchActivityListItemsDto ToSearchActivityListItemsDto(this ActivityListItemFilterViewModel filter) => new()
    {
        ListId = filter.ListId,
        SearchText = filter.SearchText,
        StatusId = filter.StatusId,
        Assigned = filter.Assigned,
        NoteFilter = filter.NoteFilter,
        SortColumn = filter.SortColumn,
        SortDir = filter.SortDir,
        Page = filter.Page,
        PageSize = filter.PageSize
    };

    public static UpdateActivityListFieldDto ToUpdateActivityListFieldDto(this ActivityListFieldUpdateViewModel model) => new()
    {
        ListId = model.ListId,
        ItemId = model.ItemId,
        Field = model.Field,
        ColumnId = model.ColumnId,
        Value = model.Value
    };

    public static SaveActivityListColumnDto ToSaveActivityListColumnDto(this ActivityListColumnSaveViewModel model) => new()
    {
        ListId = model.ListId,
        ColumnId = model.ColumnId,
        Name = model.Name,
        Kind = model.Kind,
        Options = model.Options
    };

    public static SaveActivityListStatusesDto ToSaveActivityListStatusesDto(this ActivityListStatusesSaveViewModel model) => new()
    {
        ListId = model.ListId,
        DefaultIndex = model.DefaultIndex,
        Statuses = model.Statuses.Select(s => new SaveActivityListStatusDto { Id = s.Id, Name = s.Name, Color = s.Color }).ToList()
    };

    public static SendActivityListLinksDto ToSendActivityListLinksDto(this ActivityListSendLinksViewModel model) => new()
    {
        ListId = model.ListId,
        MemberIds = model.MemberIds,
        ViaEmail = model.ViaEmail,
        ViaSms = model.ViaSms,
        Message = model.Message
    };

    public static CreateActivityListFromExcelDto ToCreateActivityListFromExcelDto(this ActivityListCreateViewModel model, Stream content, string? userId) => new()
    {
        ActivityId = model.ActivityId,
        Title = model.Title,
        Description = model.Description,
        FileName = model.File?.FileName ?? string.Empty,
        Content = content,
        UserId = userId
    };

    public static List<LabelInputViewModel> ToLabelInputViewModels(this ActivityListItemLabelsDto labels) =>
        labels.Labels.Select(l => new LabelInputViewModel { Text = l.Text, Quantity = l.Quantity }).ToList();

    /// <summary>Trimmed texts; rows without text are skipped (see <see cref="LabelRows.Normalize"/>).</summary>
    public static List<ActivityListLabelDto> ToActivityListLabelDtos(this ActivityListItemLabelsSaveViewModel model) =>
        LabelRows.Normalize(model.Labels).Select(r => new ActivityListLabelDto { Text = r.Text, Quantity = r.Quantity }).ToList();

    public static List<LabelRow> ToLabelRows(this ActivityListPrintLabelsDto labels) =>
        labels.Labels.Select(l => new LabelRow(l.Text, l.Quantity)).ToList();

    private static ActivityListSchemaViewModel ToViewModel(this ActivityListSchemaDto schema) => new()
    {
        ListId = schema.ListId,
        ShowNote = schema.ShowNote,
        SumColumnId = schema.SumColumnId,
        Columns = schema.Columns.Select(c => new ActivityListColumnViewModel
        {
            Id = c.Id,
            Name = c.Name,
            Kind = c.Kind,
            Options = c.Options,
            IsHidden = c.IsHidden
        }).ToList(),
        Statuses = schema.Statuses.Select(ToViewModel).ToList(),
        Members = schema.Members.Select(m => new ActivityListMemberViewModel
        {
            Id = m.Id,
            Name = m.Name,
            Role = m.Role,
            IsLinkedUser = m.IsLinkedUser,
            IsMe = m.IsMe,
            AssignedCount = m.AssignedCount,
            Email = m.Email,
            Mobile = m.Mobile,
            PublicId = m.PublicId
        }).ToList()
    };

    private static ActivityListStatusViewModel ToViewModel(ActivityListStatusDto status) => new()
    {
        Id = status.Id,
        Name = status.Name,
        Color = status.Color,
        IsDefault = status.IsDefault,
        Count = status.Count
    };

    private static PublicWorkListItemsViewModel FillPublicItems(ActivityListItemPageDto page, ActivityListSchemaViewModel schema)
    {
        var model = FillItems(new PublicWorkListItemsViewModel(), page, schema);
        model.MemberPublicId = page.MemberPublicId ?? Guid.Empty;
        return model;
    }

    private static T FillItems<T>(T model, ActivityListItemPageDto page, ActivityListSchemaViewModel schema) where T : ActivityListItemsViewModel
    {
        model.ListId = page.ListId;
        model.SearchText = page.SearchText;
        model.StatusId = page.StatusId;
        model.Assigned = page.Assigned;
        model.NoteFilter = page.NoteFilter;
        model.SortColumn = page.SortColumn;
        model.SortDir = page.SortDir;
        model.Page = page.Page;
        model.PageSize = page.PageSize;
        model.TotalCount = page.TotalCount;
        model.Schema = schema;
        model.Rows = page.Rows.Select(r => new ActivityListItemRowViewModel
        {
            Id = r.Id,
            RowNumber = r.RowNumber,
            StatusId = r.StatusId,
            Note = r.Note,
            AssignedMemberId = r.AssignedMemberId,
            UpdatedAtUtc = r.UpdatedAtUtc,
            UpdatedByName = r.UpdatedByName,
            Values = r.Values,
            LabelCopies = r.LabelCopies
        }).ToList();
        return model;
    }
}
