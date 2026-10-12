namespace web.ViewModels;

/// <summary>A color theme in one mode, for the theme picker's preview on Indstillinger → Tema.</summary>
public class ThemeSettingViewModel
{
    public string Name { get; set; } = string.Empty;
    public string ThemeMode { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string SurfaceColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
}
