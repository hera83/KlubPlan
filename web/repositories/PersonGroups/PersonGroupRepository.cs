using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Repositories.PersonGroups.Dtos;
using web.Repositories.PersonGroups.Interfaces;
using web.Repositories.PersonGroups.Mappers;

namespace web.Repositories.PersonGroups;

public class PersonGroupRepository(WebDbContext dbContext, ILogger<PersonGroupRepository> logger) : IPersonGroupRepository
{
    public Task<List<PersonGroupDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        dbContext.PersonGroups.AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(PersonGroupMapper.ToDtoExpression)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        dbContext.PersonGroups.CountAsync(cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.PersonGroups.AnyAsync(g => g.Id == id, cancellationToken);

    public async Task<SavePersonGroupResultDto> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        if (await dbContext.PersonGroups.AnyAsync(g => g.Name == trimmed, cancellationToken))
        {
            return new SavePersonGroupResultDto { Status = SavePersonGroupStatus.NameInUse, Name = trimmed };
        }

        var group = PersonGroupMapper.ToEntity(trimmed);
        dbContext.PersonGroups.Add(group);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Kunne ikke oprette gruppe pga. formentlig navnekollision ({Name})", trimmed);
            return new SavePersonGroupResultDto { Status = SavePersonGroupStatus.NameInUse, Name = trimmed };
        }

        return new SavePersonGroupResultDto { Status = SavePersonGroupStatus.Saved, GroupId = group.Id, Name = trimmed };
    }

    public async Task<SavePersonGroupResultDto> RenameAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var group = await dbContext.PersonGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (group is null)
        {
            return new SavePersonGroupResultDto { Status = SavePersonGroupStatus.NotFound };
        }

        var trimmed = name.Trim();
        if (await dbContext.PersonGroups.AnyAsync(g => g.Name == trimmed && g.Id != id, cancellationToken))
        {
            return new SavePersonGroupResultDto { Status = SavePersonGroupStatus.NameInUse, GroupId = id, Name = trimmed };
        }

        group.Name = trimmed;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Kunne ikke omdøbe gruppe {GroupId} pga. formentlig navnekollision ({Name})", id, trimmed);
            return new SavePersonGroupResultDto { Status = SavePersonGroupStatus.NameInUse, GroupId = id, Name = trimmed };
        }

        return new SavePersonGroupResultDto { Status = SavePersonGroupStatus.Saved, GroupId = id, Name = trimmed };
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var group = await dbContext.PersonGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (group is null)
        {
            return false;
        }

        // Cascade delete only removes the membership rows (see PersonGroupMembershipConfiguration) — people are kept.
        dbContext.PersonGroups.Remove(group);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
