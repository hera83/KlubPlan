namespace web.Repositories.People.Dtos
{
    public class UpdatePersonResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? GeneratedUid { get; set; }
    }
}
