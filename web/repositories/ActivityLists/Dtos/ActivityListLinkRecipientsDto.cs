namespace web.Repositories.ActivityLists.Dtos;

/// <summary>The selected external contacts to send their personal /Arbejdsliste link to.</summary>
public class ActivityListLinkRecipientsDto
{
    public int ListId { get; set; }
    public string ListTitle { get; set; } = string.Empty;
    public string ActivityTitle { get; set; } = string.Empty;
    public List<ActivityListLinkMemberDto> Members { get; set; } = [];
}
