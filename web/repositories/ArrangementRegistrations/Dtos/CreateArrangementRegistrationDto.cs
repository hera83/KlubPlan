namespace web.Repositories.ArrangementRegistrations.Dtos;

/// <summary>A validated sign-up to save: the person, the answers and one row per taken shift slot.</summary>
public class CreateArrangementRegistrationDto
{
    public int ArrangementId { get; set; }
    public int PersonId { get; set; }
    public List<ArrangementAnswerValueDto> Answers { get; set; } = [];
    public List<RegisteredShiftDto> Shifts { get; set; } = [];
}
