namespace web.ViewModels;

/// <summary>The arbejdsgruppe member modal on the activity page (_WorkgroupMemberModal).</summary>
public class ActivityWorkgroupMemberFormViewModel
{
    /// <summary>0 = new member.</summary>
    public int Id { get; set; }

    public int ActivityId { get; set; }

    /// <summary>Set to link the member to an administrator account instead of an external contact.</summary>
    public string? ApplicationUserId { get; set; }

    /// <summary>Required only when ApplicationUserId is not set.</summary>
    public string? Name { get; set; }

    public string? Role { get; set; }

    public string? Email { get; set; }

    public string? Mobile { get; set; }
}
