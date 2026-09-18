namespace web.Data.Entities
{
    /// <summary>Join entity: a Person explicitly selected (not just via a group) as a recipient of a CommunicationMessage.</summary>
    public class CommunicationMessageRecipientPerson
    {
        public int CommunicationMessageId { get; set; }

        public int PersonId { get; set; }

        public virtual CommunicationMessage CommunicationMessage { get; set; } = null!;

        public virtual Person Person { get; set; } = null!;
    }
}
