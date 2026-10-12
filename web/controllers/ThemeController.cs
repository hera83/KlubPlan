using System.Text;
using Microsoft.AspNetCore.Mvc;
using web.Data.Models;
using web.Repositories.AppSettings.Interfaces;
using web.Repositories.ThemeSettings.Dtos;
using web.Repositories.ThemeSettings.Interfaces;

namespace web.Controllers;

public class ThemeController(IAppSettingRepository appSettingRepository, IThemeSettingRepository themeSettingRepository) : Controller
{
    [HttpGet("theme.css")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Css(CancellationToken cancellationToken)
    {
        var activeMode = await appSettingRepository.GetValueAsync(AppSettingKeys.ActiveThemeMode, cancellationToken) ?? ThemeMode.System;
        var activeThemeName = await appSettingRepository.GetValueAsync(AppSettingKeys.ActiveThemeName, cancellationToken) ?? "Orange";

        var lightTheme = await themeSettingRepository.GetAsync(activeThemeName, ThemeMode.Light, cancellationToken);
        var darkTheme = await themeSettingRepository.GetAsync(activeThemeName, ThemeMode.Dark, cancellationToken);

        var css = new StringBuilder();

        if (activeMode == ThemeMode.Dark && darkTheme is not null)
        {
            css.AppendLine(BuildVariables(":root", darkTheme));
        }
        else if (activeMode == ThemeMode.Light && lightTheme is not null)
        {
            css.AppendLine(BuildVariables("[data-theme=\"light\"]", lightTheme));
        }
        else
        {
            if (darkTheme is not null)
            {
                css.AppendLine(BuildVariables(":root", darkTheme));
            }

            if (lightTheme is not null)
            {
                css.AppendLine(BuildVariables("[data-theme=\"light\"]", lightTheme));
            }
        }

        return Content(css.ToString(), "text/css", Encoding.UTF8);
    }

    private static string BuildVariables(string selector, ThemeSettingDto theme)
    {
        var css = new StringBuilder();
        css.AppendLine($"{selector} {{");
        css.AppendLine($"  --clr-brand: {theme.PrimaryColor};");
        css.AppendLine($"  --clr-ok: {theme.SuccessColor};");
        css.AppendLine($"  --clr-alarm: {theme.DangerColor};");
        css.AppendLine($"  --clr-warn: {theme.WarningColor};");
        css.AppendLine($"  --clr-info: {theme.InfoColor};");
        css.AppendLine($"  --clr-bg: {theme.BackgroundColor};");
        css.AppendLine($"  --clr-surface: {theme.SurfaceColor};");
        css.AppendLine($"  --clr-text: {theme.TextColor};");
        css.AppendLine($"  --clr-text-muted: {theme.TextMutedColor};");
        css.AppendLine($"  --clr-border: {theme.BorderColor};");
        css.AppendLine("}");
        return css.ToString();
    }
}
