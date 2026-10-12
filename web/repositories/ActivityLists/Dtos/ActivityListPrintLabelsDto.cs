namespace web.Repositories.ActivityLists.Dtos;

/// <summary>The labels on the lines matching a search, in line order and then label order.</summary>
public class ActivityListPrintLabelsDto
{
    public string Title { get; set; } = string.Empty;
    public List<ActivityListPrintLabelDto> Labels { get; set; } = [];

    /// <summary>Lines with at least one label.</summary>
    public int Lines => Labels.Select(l => l.ItemId).Distinct().Count();

    /// <summary>Labels on the sheets, i.e. the sum of the quantities.</summary>
    public int Copies => Labels.Sum(l => l.Quantity);
}
