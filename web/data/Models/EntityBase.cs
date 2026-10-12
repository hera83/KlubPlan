namespace web.Data.Models;

/// <summary>
/// Base for every table with its own Id. CreatedAt/UpdatedAt are UTC and set automatically in
/// <see cref="WebDbContext.SaveChangesAsync(bool, CancellationToken)"/> — never set them by hand.
/// Id stays an int (not Guid) in this project; see web/.claude/CLAUDE.md.
/// </summary>
public abstract class EntityBase
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
