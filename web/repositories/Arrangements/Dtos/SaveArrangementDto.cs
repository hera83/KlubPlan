using web.Constants;

namespace web.Repositories.Arrangements.Dtos;

/// <summary>Creates (Id 0) or updates an arrangement. Fields, shifts and requirements not in the lists are deleted; the access list is replaced.</summary>
public class SaveArrangementDto
{
    /// <summary>0 means create a new arrangement.</summary>
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>The creator, when creating.</summary>
    public string? UserId { get; set; }

    public List<SaveArrangementFormFieldDto> FormFields { get; set; } = [];
    public List<SaveArrangementShiftDto> Shifts { get; set; } = [];
    public DateTime? RegistrationOpensAtUtc { get; set; }
    public DateTime? RegistrationClosesAtUtc { get; set; }
    public ArrangementAccessMode AccessMode { get; set; } = ArrangementAccessMode.Open;

    /// <summary>Only stored when AccessMode is Restricted.</summary>
    public List<int> AllowedPersonIds { get; set; } = [];

    /// <summary>Only stored when AccessMode is Restricted.</summary>
    public List<int> AllowedGroupIds { get; set; } = [];

    public bool AllowMultipleNamesPerShift { get; set; }
}
