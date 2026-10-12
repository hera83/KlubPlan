namespace web.Repositories.InfoScreens.Dtos;

/// <summary>Outcome of creating/saving/(de)activating a screen.</summary>
public class SaveInfoScreenResultDto
{
    public SaveInfoScreenStatus Status { get; set; }
    public int Id { get; set; }

    /// <summary>The screen's (saved) title, for the toast.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Set when Status is Invalid.</summary>
    public string? ErrorMessage { get; set; }

    public static SaveInfoScreenResultDto Saved(int id, string title) => new() { Status = SaveInfoScreenStatus.Saved, Id = id, Title = title };
    public static SaveInfoScreenResultDto NotFound() => new() { Status = SaveInfoScreenStatus.NotFound };
    public static SaveInfoScreenResultDto Invalid(string error) => new() { Status = SaveInfoScreenStatus.Invalid, ErrorMessage = error };
}
