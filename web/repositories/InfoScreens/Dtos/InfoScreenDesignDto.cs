namespace web.Repositories.InfoScreens.Dtos;

/// <summary>
/// Everything the designer edits and saves in one go ("Gem"): the screen's settings and all its
/// slides. Sent as JSON (camelCase, InfoScreenMapper.JsonOptions) both ways; the server validates
/// and normalizes it on save.
/// </summary>
public class InfoScreenDesignDto
{
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string AspectRatio { get; set; } = "16:9";
    public string Transition { get; set; } = "fade";
    public List<InfoScreenSlideDto> Slides { get; set; } = new();
}
