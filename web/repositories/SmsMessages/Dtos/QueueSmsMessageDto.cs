namespace web.Repositories.SmsMessages.Dtos;

/// <summary>An outbound SMS to put in the queue SmsWorker sends.</summary>
public class QueueSmsMessageDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
