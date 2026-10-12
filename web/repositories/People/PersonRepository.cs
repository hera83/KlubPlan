using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Models;
using web.Repositories.People.Dtos;
using web.Repositories.People.Interfaces;
using web.Repositories.People.Mappers;

namespace web.Repositories.People;

public class PersonRepository(WebDbContext dbContext, ILogger<PersonRepository> logger) : IPersonRepository
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 500;

    public async Task<PersonPageDto> SearchAsync(SearchPeopleDto search, CancellationToken cancellationToken = default)
    {
        var page = search.Page < 1 ? 1 : search.Page;
        var pageSize = search.PageSize is < DefaultPageSize or > MaxPageSize ? DefaultPageSize : search.PageSize;

        var query = dbContext.People.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var text = search.SearchText.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(text) ||
                p.Uid.ToLower().Contains(text) ||
                (p.Mobile != null && p.Mobile.ToLower().Contains(text)) ||
                (p.Email != null && p.Email.ToLower().Contains(text)));
        }

        if (search.GroupIds.Count > 0)
        {
            var groupIds = search.GroupIds;
            query = query.Where(p => p.Memberships.Any(m => groupIds.Contains(m.GroupId)));
        }

        if (search.Type.HasValue)
        {
            var type = search.Type.Value;
            query = query.Where(p => p.Memberships.Any(m => m.Type == type));
        }

        return new PersonPageDto
        {
            TotalCount = await query.CountAsync(cancellationToken),
            Items = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(PersonMapper.ToListItemDtoExpression)
                .ToListAsync(cancellationToken),
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<PersonDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.People.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(PersonMapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<PersonOptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default) =>
        dbContext.People.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new PersonOptionDto { Id = p.Id, Uid = p.Uid, Name = p.Name })
            .ToListAsync(cancellationToken);

    public Task<int> CountInGroupsAsync(IReadOnlyCollection<int> groupIds, CancellationToken cancellationToken = default) =>
        groupIds.Count == 0
            ? Task.FromResult(0)
            : dbContext.People.CountAsync(p => p.Memberships.Any(m => groupIds.Contains(m.GroupId)), cancellationToken);

    public async Task<List<PersonContactDto>> GetContactsAsync(IReadOnlyCollection<int> groupIds, IReadOnlyCollection<int> personIds, CancellationToken cancellationToken = default)
    {
        if (groupIds.Count == 0 && personIds.Count == 0)
        {
            return [];
        }

        return await dbContext.People.AsNoTracking()
            .Where(p => personIds.Contains(p.Id) || p.Memberships.Any(m => groupIds.Contains(m.GroupId)))
            .OrderBy(p => p.Id)
            .Select(p => new PersonContactDto
            {
                Id = p.Id,
                PublicId = p.PublicId,
                Name = p.Name,
                Email = p.Email,
                Mobile = p.Mobile,
                Guardians = p.Guardians
                    .OrderBy(g => g.Order).ThenBy(g => g.Id)
                    .Select(g => new PersonContactGuardianDto { Order = g.Order, Email = g.Email, Mobile = g.Mobile })
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsInAnyGroupAsync(int personId, IReadOnlyCollection<int> groupIds, CancellationToken cancellationToken = default) =>
        dbContext.PersonGroupMemberships.AnyAsync(m => m.PersonId == personId && groupIds.Contains(m.GroupId), cancellationToken);

    public Task<int?> GetIdByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        dbContext.People.AsNoTracking()
            .Where(p => p.PublicId == publicId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<SavePersonResultDto> CreateAsync(CreatePersonDto dto, CancellationToken cancellationToken = default)
    {
        var uid = string.IsNullOrWhiteSpace(dto.Uid) ? await GenerateUidAsync(cancellationToken) : dto.Uid.Trim();
        if (await UidExistsAsync(uid, null, cancellationToken))
        {
            return new SavePersonResultDto { Status = SavePersonStatus.UidInUse, Uid = uid };
        }

        var person = dto.ToEntity(uid);
        dbContext.People.Add(person);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Kunne ikke oprette person pga. formentlig UID-kollision ({Uid})", uid);
            return new SavePersonResultDto { Status = SavePersonStatus.UidInUse, Uid = uid };
        }

        return new SavePersonResultDto { Status = SavePersonStatus.Saved, PersonId = person.Id, Uid = uid };
    }

    public async Task<SavePersonResultDto> UpdateAsync(int id, UpdatePersonDto dto, CancellationToken cancellationToken = default)
    {
        var person = await dbContext.People
            .Include(p => p.Memberships)
            .Include(p => p.Guardians)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (person is null)
        {
            return new SavePersonResultDto { Status = SavePersonStatus.NotFound };
        }

        var uid = string.IsNullOrWhiteSpace(dto.Uid) ? await GenerateUidAsync(cancellationToken) : dto.Uid.Trim();
        if (await UidExistsAsync(uid, person.Id, cancellationToken))
        {
            return new SavePersonResultDto { Status = SavePersonStatus.UidInUse, PersonId = id, Uid = uid };
        }

        // Memberships and guardians are replaced wholesale — simple and correct for lists of this size.
        dbContext.PersonGroupMemberships.RemoveRange(person.Memberships);
        person.Memberships.Clear();
        dbContext.PersonGuardians.RemoveRange(person.Guardians);
        person.Guardians.Clear();

        dto.ApplyTo(person, uid);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Kunne ikke opdatere person {PersonId} pga. formentlig UID-kollision ({Uid})", id, uid);
            return new SavePersonResultDto { Status = SavePersonStatus.UidInUse, PersonId = id, Uid = uid };
        }

        return new SavePersonResultDto { Status = SavePersonStatus.Saved, PersonId = id, Uid = uid };
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var person = await dbContext.People.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (person is null)
        {
            return false;
        }

        dbContext.People.Remove(person);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RegeneratePublicIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var person = await dbContext.People.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (person is null)
        {
            return false;
        }

        person.PublicId = Guid.NewGuid();
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Person {PersonId} public-id regenerated (e.g. link revoked)", id);
        return true;
    }

    public async Task<int?> CreateManyInGroupAsync(int groupId, IReadOnlyList<string> names, CancellationToken cancellationToken = default)
    {
        var nextUidNumber = await GetNextUidNumberAsync(cancellationToken);
        var people = new List<Person>();

        foreach (var name in names)
        {
            var person = new Person { Uid = FormatUid(nextUidNumber++), Name = name };
            person.Memberships.Add(new PersonGroupMembership { GroupId = groupId, Type = PersonType.Player });
            people.Add(person);
        }

        dbContext.People.AddRange(people);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Kunne ikke importere personer til gruppe {GroupId}", groupId);
            return null;
        }

        return people.Count;
    }

    private async Task<string> GenerateUidAsync(CancellationToken cancellationToken) =>
        FormatUid(await GetNextUidNumberAsync(cancellationToken));

    private static string FormatUid(int number) => number.ToString("D6");

    private async Task<int> GetNextUidNumberAsync(CancellationToken cancellationToken)
    {
        var existingUids = await dbContext.People.AsNoTracking().Select(p => p.Uid).ToListAsync(cancellationToken);
        return existingUids
            .Select(uid => int.TryParse(uid, out var numeric) ? numeric : (int?)null)
            .Where(numeric => numeric.HasValue)
            .Select(numeric => numeric!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }

    private Task<bool> UidExistsAsync(string uid, int? excludePersonId, CancellationToken cancellationToken) =>
        dbContext.People.AnyAsync(p => p.Uid == uid && (excludePersonId == null || p.Id != excludePersonId), cancellationToken);
}
