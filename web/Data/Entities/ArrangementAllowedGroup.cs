namespace web.Data.Entities
{
    /// <summary>Join entity: a PersonGroup whose members are allowed to register when Arrangement.AccessMode is Restricted.</summary>
    public class ArrangementAllowedGroup
    {
        public int ArrangementId { get; set; }

        public int PersonGroupId { get; set; }

        public virtual Arrangement Arrangement { get; set; } = null!;

        public virtual PersonGroup PersonGroup { get; set; } = null!;
    }
}
