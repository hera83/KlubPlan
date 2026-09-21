namespace web.Repositories.Activities.Dtos
{
    /// <summary>Shared success/error result for the small arbejdsgruppe/opgave/link mutations.</summary>
    public class ActivityActionResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
