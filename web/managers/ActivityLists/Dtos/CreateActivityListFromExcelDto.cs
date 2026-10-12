namespace web.Managers.ActivityLists.Dtos;

public class CreateActivityListFromExcelDto
{
    public int ActivityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;

    /// <summary>The uploaded .xlsx — must be seekable (ClosedXML reads it as a zip).</summary>
    public Stream Content { get; set; } = Stream.Null;
    public string? UserId { get; set; }
}
