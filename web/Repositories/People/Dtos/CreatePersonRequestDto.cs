using web.Constants;
using web.ViewModels;

namespace web.Repositories.People.Dtos
{
    public class CreatePersonRequestDto
    {
        public string? Uid { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public List<int> GroupIds { get; set; } = new();
        public Dictionary<int, PersonType> GroupTypes { get; set; } = new();
        public List<PersonGuardianViewModel> Guardians { get; set; } = new();
    }
}
