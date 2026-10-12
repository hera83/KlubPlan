namespace web.Repositories.SmsMessages.Dtos;

public class SearchSmsMessagesDto
{
    /// <summary>Matches phone number, text or status.</summary>
    public string? SearchText { get; set; }
    public string? Direction { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
