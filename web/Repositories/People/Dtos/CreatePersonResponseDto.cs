namespace web.Repositories.People.Dtos
{
    public class CreatePersonResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int PersonId { get; set; }
        public string? GeneratedUid { get; set; }
    }
}
