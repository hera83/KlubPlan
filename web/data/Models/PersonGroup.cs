namespace web.Data.Models;

/// <summary>
/// A named group persons can be assigned to (e.g. a team or a committee). A person can belong
/// to multiple groups. Deleting a group only removes memberships, never the persons in it.
/// </summary>
public class PersonGroup : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<PersonGroupMembership> Memberships { get; set; } = new List<PersonGroupMembership>();
}
