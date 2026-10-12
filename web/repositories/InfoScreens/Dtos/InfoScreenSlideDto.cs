namespace web.Repositories.InfoScreens.Dtos;

public class InfoScreenSlideDto
{
    public int DurationSeconds { get; set; } = 10;
    public string Background { get; set; } = "#111827";
    public bool IsHidden { get; set; }
    public List<InfoScreenElementDto> Elements { get; set; } = new();
}
