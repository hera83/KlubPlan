namespace web.Managers.Meetings.Dtos;

public enum GenerateSummaryStatus
{
    Generated,
    NotFound,

    /// <summary>Neither agenda nor minutes have any text.</summary>
    NoNotes,

    /// <summary>No AI chat model is configured.</summary>
    NoModel,

    /// <summary>The AI gateway answered with an empty text.</summary>
    EmptyAnswer,

    /// <summary>The AI gateway failed or couldn't be reached. ErrorMessage says why.</summary>
    Failed
}
