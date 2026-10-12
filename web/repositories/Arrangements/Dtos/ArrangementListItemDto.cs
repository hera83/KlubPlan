namespace web.Repositories.Arrangements.Dtos;

public class ArrangementListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>The earliest shift's start, or null without shifts.</summary>
    public DateTime? EventDateUtc { get; set; }

    /// <summary>Sum of the shifts' needed counts.</summary>
    public int NeededCount { get; set; }

    /// <summary>Taken shift slots (registrations incl. named companions).</summary>
    public int RegistrationCount { get; set; }

    public DateTime? RegistrationOpensAtUtc { get; set; }
    public DateTime? RegistrationClosesAtUtc { get; set; }
    public bool RegistrationForcedOpen { get; set; }
    public DateTime CreatedAt { get; set; }
}
