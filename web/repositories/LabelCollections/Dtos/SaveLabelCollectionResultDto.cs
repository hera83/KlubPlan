namespace web.Repositories.LabelCollections.Dtos;

/// <summary>Outcome of creating/saving a collection.</summary>
public class SaveLabelCollectionResultDto
{
    public SaveLabelCollectionStatus Status { get; set; }
    public int Id { get; set; }

    /// <summary>The collection's (saved) name, for the toast.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Set when Status is Invalid.</summary>
    public string? ErrorMessage { get; set; }

    public static SaveLabelCollectionResultDto Saved(int id, string name) => new() { Status = SaveLabelCollectionStatus.Saved, Id = id, Name = name };
    public static SaveLabelCollectionResultDto NotFound() => new() { Status = SaveLabelCollectionStatus.NotFound };
    public static SaveLabelCollectionResultDto Invalid(string error) => new() { Status = SaveLabelCollectionStatus.Invalid, ErrorMessage = error };
}
