namespace web.Data.Entities
{
    /// <summary>Join entity: a Person explicitly allowed to register when Arrangement.AccessMode is Restricted.</summary>
    public class ArrangementAllowedPerson
    {
        public int ArrangementId { get; set; }

        public int PersonId { get; set; }

        public virtual Arrangement Arrangement { get; set; } = null!;

        public virtual Person Person { get; set; } = null!;
    }
}
