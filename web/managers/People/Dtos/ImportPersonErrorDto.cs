namespace web.Managers.People.Dtos;

/// <summary>A line from the import that was rejected, and why.</summary>
public class ImportPersonErrorDto
{
    public string Name { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
