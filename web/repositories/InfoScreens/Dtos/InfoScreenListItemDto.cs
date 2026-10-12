namespace web.Repositories.InfoScreens.Dtos;

public class InfoScreenListItemDto
{
    public int Id { get; set; }
    public Guid PublicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SlideCount { get; set; }

    /// <summary>Slides that are played (not hidden).</summary>
    public int VisibleSlideCount { get; set; }

    /// <summary>One round of the slideshow (visible slides).</summary>
    public int TotalSeconds { get; set; }

    public DateTime UpdatedAt { get; set; }
}
