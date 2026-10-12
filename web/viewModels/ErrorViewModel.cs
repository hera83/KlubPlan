namespace web.ViewModels;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <summary>The unhandled exception (type, message and stack trace). Only set in Development.</summary>
    public string? ExceptionDetails { get; set; }
}
