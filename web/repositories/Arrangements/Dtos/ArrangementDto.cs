using web.Constants;

namespace web.Repositories.Arrangements.Dtos;

/// <summary>One arrangement with its sign-up form fields, shifts (with requirements) and access list. Times are UTC.</summary>
public class ArrangementDto
{
    public int Id { get; set; }
    public Guid PublicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? RegistrationOpensAtUtc { get; set; }
    public DateTime? RegistrationClosesAtUtc { get; set; }

    /// <summary>Registration temporarily forced open, overriding the dates.</summary>
    public bool RegistrationForcedOpen { get; set; }

    public ArrangementAccessMode AccessMode { get; set; }
    public bool AllowMultipleNamesPerShift { get; set; }

    /// <summary>In order.</summary>
    public List<ArrangementFormFieldDto> FormFields { get; set; } = [];

    /// <summary>By start time, then order.</summary>
    public List<ArrangementShiftDto> Shifts { get; set; } = [];

    public List<int> AllowedPersonIds { get; set; } = [];
    public List<int> AllowedGroupIds { get; set; } = [];
}
