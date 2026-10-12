namespace web.Repositories.ThemeSettings.Dtos;

/// <summary>One color theme in one mode (Light or Dark).</summary>
public class ThemeSettingDto
{
    public string Name { get; set; } = string.Empty;
    public string ThemeMode { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = string.Empty;
    public string SuccessColor { get; set; } = string.Empty;
    public string DangerColor { get; set; } = string.Empty;
    public string WarningColor { get; set; } = string.Empty;
    public string InfoColor { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string SurfaceColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string TextMutedColor { get; set; } = string.Empty;
    public string BorderColor { get; set; } = string.Empty;
}
