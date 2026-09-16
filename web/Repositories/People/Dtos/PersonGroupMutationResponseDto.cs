namespace web.Repositories.People.Dtos
{
    public class PersonGroupMutationResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int GroupId { get; set; }
    }
}
