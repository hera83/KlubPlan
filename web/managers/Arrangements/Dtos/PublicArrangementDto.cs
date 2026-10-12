using web.Constants;
using web.Repositories.Arrangements.Dtos;

namespace web.Managers.Arrangements.Dtos;

/// <summary>The public /Tilmelding link's gate result, the arrangement when it exists, and (when Ok) who signs up and how full each shift is.</summary>
public class PublicArrangementDto
{
    public PublicArrangementStatus Status { get; set; }
    public ArrangementDto? Arrangement { get; set; }
    public string? PersonName { get; set; }

    /// <summary>Taken slots per shift id (only filled when Status is Ok).</summary>
    public Dictionary<int, int> TakenCountByShift { get; set; } = [];
}
