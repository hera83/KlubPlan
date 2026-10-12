namespace web.Repositories.Activities.Dtos;

/// <summary>A member of the arbejdsgruppe — either linked to an administrator account or an external contact.</summary>
public class ActivityWorkgroupMemberDto
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public int Order { get; set; }
    public string? ApplicationUserId { get; set; }

    /// <summary>The linked account's name (null when not linked, or the account is gone).</summary>
    public string? UserDisplayName { get; set; }

    /// <summary>The external contact's name (null when linked to an account).</summary>
    public string? Name { get; set; }
    public string? Role { get; set; }

    /// <summary>The account's e-mail when linked, otherwise the external contact's.</summary>
    public string? Email { get; set; }

    /// <summary>The account's phone number when linked, otherwise the external contact's.</summary>
    public string? Mobile { get; set; }
}
