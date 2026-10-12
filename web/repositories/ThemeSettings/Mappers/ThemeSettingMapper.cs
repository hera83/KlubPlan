using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.ThemeSettings.Dtos;

namespace web.Repositories.ThemeSettings.Mappers;

public static class ThemeSettingMapper
{
    public static readonly Expression<Func<ThemeSetting, ThemeSettingDto>> ToDtoExpression = x => new ThemeSettingDto
    {
        Name = x.Name,
        ThemeMode = x.ThemeMode,
        PrimaryColor = x.PrimaryColor,
        SuccessColor = x.SuccessColor,
        DangerColor = x.DangerColor,
        WarningColor = x.WarningColor,
        InfoColor = x.InfoColor,
        BackgroundColor = x.BackgroundColor,
        SurfaceColor = x.SurfaceColor,
        TextColor = x.TextColor,
        TextMutedColor = x.TextMutedColor,
        BorderColor = x.BorderColor
    };
}
