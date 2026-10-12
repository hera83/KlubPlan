namespace web.Repositories.InfoScreens.Dtos;

/// <summary>What the designer opens with: the saved design and the media library (newest first).</summary>
public class InfoScreenDesignerDto
{
    public int Id { get; set; }
    public Guid PublicId { get; set; }
    public InfoScreenDesignDto Design { get; set; } = new();
    public List<InfoScreenMediaDto> Media { get; set; } = [];
}
