namespace web.Managers.Arrangements.Dtos;

/// <summary>What the public sign-up form posts.</summary>
public class SubmitPublicArrangementDto
{
    public List<ArrangementAnswerInputDto> Answers { get; set; } = [];

    /// <summary>ArrangementShift ids the registrant selected.</summary>
    public List<int> SelectedShiftIds { get; set; } = [];

    /// <summary>ArrangementShiftRequirement ids the registrant ticked to self-declare they meet them.</summary>
    public List<int> ConfirmedRequirementIds { get; set; } = [];

    /// <summary>Named slots per shift — only honored when the arrangement allows it.</summary>
    public List<ArrangementCompanionInputDto> Companions { get; set; } = [];
}
