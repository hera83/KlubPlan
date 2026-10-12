namespace web.Managers.Arrangements.Dtos;

/// <summary>A named slot on a shift, as posted when the arrangement allows several names per shift.</summary>
public class ArrangementCompanionInputDto
{
    public int ShiftId { get; set; }
    public string? Name { get; set; }
}
