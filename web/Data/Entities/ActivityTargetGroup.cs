namespace web.Data.Entities
{
    /// <summary>Join entity: a PersonGroup that belongs to an Activity's target audience (målgruppe).</summary>
    public class ActivityTargetGroup
    {
        public int ActivityId { get; set; }

        public int PersonGroupId { get; set; }

        public virtual Activity Activity { get; set; } = null!;

        public virtual PersonGroup PersonGroup { get; set; } = null!;
    }
}
