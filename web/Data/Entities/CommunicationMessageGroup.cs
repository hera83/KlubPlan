namespace web.Data.Entities
{
    /// <summary>Join entity: a PersonGroup selected as a recipient of a CommunicationMessage.</summary>
    public class CommunicationMessageGroup
    {
        public int CommunicationMessageId { get; set; }

        public int PersonGroupId { get; set; }

        public virtual CommunicationMessage CommunicationMessage { get; set; } = null!;

        public virtual PersonGroup PersonGroup { get; set; } = null!;
    }
}
