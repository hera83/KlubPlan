namespace web.Managers.People.Dtos;

public enum ImportPersonsStatus
{
    /// <summary>Done. ImportedCount may be 0 when every line was rejected (see Errors).</summary>
    Imported,
    GroupNotFound,

    /// <summary>The input contained no names at all.</summary>
    NoNames,

    /// <summary>The save failed; nothing was imported.</summary>
    Failed
}
