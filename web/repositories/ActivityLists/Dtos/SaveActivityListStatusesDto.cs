namespace web.Repositories.ActivityLists.Dtos;

public class SaveActivityListStatusesDto
{
    public int ListId { get; set; }
    public List<SaveActivityListStatusDto> Statuses { get; set; } = [];

    /// <summary>Index in Statuses of the status new lines get.</summary>
    public int DefaultIndex { get; set; }
}
