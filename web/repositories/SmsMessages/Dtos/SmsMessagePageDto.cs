namespace web.Repositories.SmsMessages.Dtos;

public class SmsMessagePageDto
{
    public List<SmsMessageDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
