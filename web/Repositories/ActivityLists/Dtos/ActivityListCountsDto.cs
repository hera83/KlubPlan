namespace web.Repositories.ActivityLists.Dtos
{
    public class ActivityListCountsDto
    {
        public int Total { get; set; }
        public int Unassigned { get; set; }
        public Dictionary<int, int> StatusCounts { get; set; } = new();
        public Dictionary<int, int> MemberCounts { get; set; } = new();
    }
}
