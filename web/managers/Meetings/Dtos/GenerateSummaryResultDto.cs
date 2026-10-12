namespace web.Managers.Meetings.Dtos;

public class GenerateSummaryResultDto
{
    public GenerateSummaryStatus Status { get; set; }
    public string? Summary { get; set; }

    /// <summary>Set when Status is Failed.</summary>
    public string? ErrorMessage { get; set; }
}
