namespace web.Repositories.Activities.Dtos
{
    /// <summary>Result of checking whether a form already has responses, before linking it to an Activity.</summary>
    public class FormResponseCheckDto
    {
        public bool HasResponses { get; set; }
        public int ResponseCount { get; set; }
    }
}
