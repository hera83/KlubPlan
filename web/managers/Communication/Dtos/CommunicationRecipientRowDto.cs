namespace web.Managers.Communication.Dtos;

/// <summary>One person in a message's target audience — also people without contact info, so gaps are visible.</summary>
public class CommunicationRecipientRowDto
{
    public string DisplayName { get; set; } = string.Empty;
    public List<CommunicationRecipientAddressDto> EmailAddresses { get; set; } = [];
    public List<CommunicationRecipientAddressDto> SmsAddresses { get; set; } = [];
    public bool HasEmailContact { get; set; }
    public bool HasSmsContact { get; set; }
}
