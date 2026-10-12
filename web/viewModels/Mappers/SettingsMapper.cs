using web.Repositories.SmsMessages.Dtos;
using web.Repositories.ThemeSettings.Dtos;
using web.Repositories.Users.Dtos;

namespace web.ViewModels.Mappers;

public static class SettingsMapper
{
    public static SearchUsersDto ToSearchUsersDto(this UserFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        Role = filter.Role,
        IsActive = filter.IsActive,
        Page = filter.Page,
        PageSize = filter.PageSize
    };

    /// <summary>Fills the filter (the search echoes back) with the page of users.</summary>
    public static UserFilterViewModel ApplyTo(this UserPageDto page, UserFilterViewModel filter, string? currentUserId)
    {
        filter.Page = page.Page;
        filter.PageSize = page.PageSize;
        filter.CurrentUserId = currentUserId;
        filter.TotalCount = page.TotalCount;
        filter.Users = page.Items.Select(u => new UserListItemViewModel
        {
            Id = u.Id,
            Email = u.Email,
            DisplayName = u.DisplayName,
            PhoneNumber = u.PhoneNumber,
            IsActive = u.IsActive,
            CreatedAtUtc = u.CreatedAtUtc,
            Roles = u.Roles
        }).ToList();
        return filter;
    }

    public static SearchSmsMessagesDto ToSearchSmsMessagesDto(this SmsFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        Direction = filter.Direction,
        Page = filter.Page,
        PageSize = filter.PageSize
    };

    /// <summary>Fills the filter (the search echoes back) with the page of SMS messages.</summary>
    public static SmsFilterViewModel ApplyTo(this SmsMessagePageDto page, SmsFilterViewModel filter)
    {
        filter.Page = page.Page;
        filter.PageSize = page.PageSize;
        filter.TotalCount = page.TotalCount;
        filter.Items = page.Items.Select(m => new SmsListItemViewModel
        {
            Id = m.Id,
            GatewayMessageId = m.GatewayMessageId,
            Direction = m.Direction,
            PhoneNumber = m.PhoneNumber,
            Body = m.Body,
            Status = m.Status,
            SegmentCount = m.SegmentCount,
            TotalPriceDkk = m.TotalPriceDkk,
            FailureReason = m.FailureReason,
            CreatedAtUtc = m.CreatedAt
        }).ToList();
        return filter;
    }

    public static List<ThemeSettingViewModel> ToThemeSettingViewModels(this List<ThemeSettingDto> themes) =>
        themes.Select(t => new ThemeSettingViewModel
        {
            Name = t.Name,
            ThemeMode = t.ThemeMode,
            PrimaryColor = t.PrimaryColor,
            BackgroundColor = t.BackgroundColor,
            SurfaceColor = t.SurfaceColor,
            TextColor = t.TextColor
        }).ToList();
}
