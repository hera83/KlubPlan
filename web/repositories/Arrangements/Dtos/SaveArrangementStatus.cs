namespace web.Repositories.Arrangements.Dtos;

public enum SaveArrangementStatus
{
    Saved,
    NotFound,

    /// <summary>A rule was broken (title, times, labels, options). ErrorMessage says which.</summary>
    Invalid
}
