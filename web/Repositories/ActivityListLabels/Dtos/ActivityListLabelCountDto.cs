namespace web.Repositories.ActivityListLabels.Dtos
{
    /// <summary>What "Print labels" would print — shown in the modal before the PDF is made.</summary>
    public class ActivityListLabelCountDto
    {
        /// <summary>Lines with at least one label.</summary>
        public int Lines { get; set; }

        /// <summary>Labels on the sheets, i.e. the sum of the quantities.</summary>
        public int Copies { get; set; }
    }
}
