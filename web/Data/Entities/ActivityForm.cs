namespace web.Data.Entities
{
    /// <summary>Join entity: en Form linket til en Activity (flere kan linkes til samme aktivitet).</summary>
    public class ActivityForm
    {
        public int ActivityId { get; set; }

        public int FormId { get; set; }

        public virtual Activity Activity { get; set; } = null!;

        public virtual Form Form { get; set; } = null!;
    }
}
