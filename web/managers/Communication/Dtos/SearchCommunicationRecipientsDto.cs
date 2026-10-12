namespace web.Managers.Communication.Dtos;

public class SearchCommunicationRecipientsDto
{
    public string? SearchText { get; set; }

    /// <summary>"MissingEmail", "MissingSms", "Sent" or empty for all.</summary>
    public string? ContactStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
