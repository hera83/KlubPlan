namespace web.Managers.ActivityLists.Dtos;

public class SendActivityListLinksDto
{
    public int ListId { get; set; }
    public List<int> MemberIds { get; set; } = [];
    public bool ViaEmail { get; set; }
    public bool ViaSms { get; set; }

    /// <summary>Optional text put before the link.</summary>
    public string? Message { get; set; }
}
