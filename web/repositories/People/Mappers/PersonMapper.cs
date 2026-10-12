using System.Linq.Expressions;
using web.Constants;
using web.Data.Models;
using web.Repositories.People.Dtos;

namespace web.Repositories.People.Mappers;

public static class PersonMapper
{
    public static readonly Expression<Func<Person, PersonDto>> ToDtoExpression = x => new PersonDto
    {
        Id = x.Id,
        Uid = x.Uid,
        Name = x.Name,
        BirthDate = x.BirthDate,
        Mobile = x.Mobile,
        Email = x.Email,
        Memberships = x.Memberships
            .Select(m => new PersonMembershipDto { GroupId = m.GroupId, GroupName = m.Group.Name, Type = m.Type })
            .ToList(),
        Guardians = x.Guardians
            .OrderBy(g => g.Order)
            .Select(g => new PersonGuardianDto { Id = g.Id, Name = g.Name, Mobile = g.Mobile, Email = g.Email })
            .ToList()
    };

    public static readonly Expression<Func<Person, PersonListItemDto>> ToListItemDtoExpression = x => new PersonListItemDto
    {
        Id = x.Id,
        Uid = x.Uid,
        Name = x.Name,
        BirthDate = x.BirthDate,
        Mobile = x.Mobile,
        Email = x.Email,
        Memberships = x.Memberships
            .OrderBy(m => m.Group.Name)
            .Select(m => new PersonMembershipDto { GroupId = m.GroupId, GroupName = m.Group.Name, Type = m.Type })
            .ToList(),
        GuardianCount = x.Guardians.Count
    };

    private static readonly Func<Person, PersonDto> CompiledToDto = ToDtoExpression.Compile();

    public static PersonDto ToDto(this Person entity) => CompiledToDto(entity);

    public static Person ToEntity(this CreatePersonDto dto, string uid)
    {
        var person = new Person
        {
            Uid = uid,
            Name = dto.Name.Trim(),
            BirthDate = dto.BirthDate,
            Mobile = NullIfBlank(dto.Mobile),
            Email = NullIfBlank(dto.Email)
        };

        AddMemberships(person, dto.GroupIds, dto.GroupTypes);
        AddGuardians(person, dto.Guardians);
        return person;
    }

    /// <summary>Sets the scalar fields and adds the new memberships/guardians. The caller removes the old ones first.</summary>
    public static void ApplyTo(this UpdatePersonDto dto, Person entity, string uid)
    {
        entity.Uid = uid;
        entity.Name = dto.Name.Trim();
        entity.BirthDate = dto.BirthDate;
        entity.Mobile = NullIfBlank(dto.Mobile);
        entity.Email = NullIfBlank(dto.Email);

        AddMemberships(entity, dto.GroupIds, dto.GroupTypes);
        AddGuardians(entity, dto.Guardians);
    }

    private static void AddMemberships(Person person, List<int> groupIds, Dictionary<int, PersonType> groupTypes)
    {
        foreach (var groupId in groupIds.Distinct())
        {
            var type = groupTypes.TryGetValue(groupId, out var t) ? t : PersonType.Player;
            person.Memberships.Add(new PersonGroupMembership { GroupId = groupId, Type = type });
        }
    }

    private static void AddGuardians(Person person, List<PersonGuardianDto> guardians)
    {
        var order = 0;
        foreach (var guardian in guardians.Where(g => !string.IsNullOrWhiteSpace(g.Name)))
        {
            person.Guardians.Add(new PersonGuardian
            {
                Name = guardian.Name.Trim(),
                Mobile = NullIfBlank(guardian.Mobile),
                Email = NullIfBlank(guardian.Email),
                Order = order++
            });
        }
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
