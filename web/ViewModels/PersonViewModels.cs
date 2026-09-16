using System.ComponentModel.DataAnnotations;

namespace web.ViewModels
{
    public class PersonFilterViewModel
    {
        public string? SearchText { get; set; }
        public int? GroupId { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public List<PersonListItemViewModel> People { get; set; } = new();
        public int TotalCount { get; set; }

        /// <summary>All groups, for the filter dropdown.</summary>
        public List<PersonGroupOptionViewModel> Groups { get; set; } = new();
    }

    public class PersonGroupOptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PersonListItemViewModel
    {
        public int Id { get; set; }
        public string Uid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public List<string> GroupNames { get; set; } = new();
        public int GuardianCount { get; set; }
    }

    public class PersonGuardianViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Navn er påkrævet")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Ugyldigt telefonnummer")]
        [StringLength(20, ErrorMessage = "Telefonnummer må ikke være længere end 20 tegn")]
        public string? Mobile { get; set; }

        [EmailAddress(ErrorMessage = "Ugyldig email-adresse")]
        [StringLength(256)]
        public string? Email { get; set; }
    }

    public class PersonDetailViewModel
    {
        public int Id { get; set; }
        public string Uid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public List<int> GroupIds { get; set; } = new();
        public List<PersonGuardianViewModel> Guardians { get; set; } = new();
    }

    public class CreatePersonViewModel
    {
        [StringLength(50, ErrorMessage = "UID må ikke være længere end 50 tegn")]
        public string? Uid { get; set; }

        [Required(ErrorMessage = "Navn er påkrævet")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public DateOnly? BirthDate { get; set; }

        [Phone(ErrorMessage = "Ugyldigt telefonnummer")]
        [StringLength(20, ErrorMessage = "Telefonnummer må ikke være længere end 20 tegn")]
        public string? Mobile { get; set; }

        [EmailAddress(ErrorMessage = "Ugyldig email-adresse")]
        [StringLength(256)]
        public string? Email { get; set; }

        public List<int> GroupIds { get; set; } = new();

        public List<PersonGuardianViewModel> Guardians { get; set; } = new();
    }

    public class EditPersonViewModel : CreatePersonViewModel
    {
        [Required]
        public int Id { get; set; }
    }

    public class PersonGroupViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class CreatePersonGroupViewModel
    {
        [Required(ErrorMessage = "Navn er påkrævet")]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    public class EditPersonGroupViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Navn er påkrævet")]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    public class ImportPersonsToGroupViewModel
    {
        [Required]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Angiv mindst ét navn")]
        public string RawNames { get; set; } = string.Empty;
    }
}
