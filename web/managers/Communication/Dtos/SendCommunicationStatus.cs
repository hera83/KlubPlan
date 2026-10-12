namespace web.Managers.Communication.Dtos;

public enum SendCommunicationStatus
{
    Saved,
    Sent,
    SubjectRequired,
    ChannelRequired,
    RecipientRequired,
    NotFound,
    NotDraft,
    EmptyBody,
    NoRecipients
}
