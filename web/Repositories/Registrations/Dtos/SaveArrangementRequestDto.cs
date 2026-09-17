using web.Constants;

namespace web.Repositories.Registrations.Dtos
{
    public class SaveArrangementFormFieldDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? HelpText { get; set; }
        public FormFieldType FieldType { get; set; }
        public bool IsRequired { get; set; }
        public int Order { get; set; }
        public string? OptionsJson { get; set; }
    }

    public class SaveArrangementShiftRequirementDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class SaveArrangementShiftDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string? Location { get; set; }
        public int NeededCount { get; set; }
        public int Order { get; set; }
        public List<SaveArrangementShiftRequirementDto> Requirements { get; set; } = new();
    }

    public class SaveArrangementRequestDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? UserId { get; set; }

        public List<SaveArrangementFormFieldDto> FormFields { get; set; } = new();
        public List<SaveArrangementShiftDto> Shifts { get; set; } = new();

        public DateTime? RegistrationOpensAtUtc { get; set; }
        public DateTime? RegistrationClosesAtUtc { get; set; }

        public ArrangementAccessMode AccessMode { get; set; } = ArrangementAccessMode.Open;
        public List<int> AllowedPersonIds { get; set; } = new();
        public List<int> AllowedGroupIds { get; set; } = new();
    }
}
