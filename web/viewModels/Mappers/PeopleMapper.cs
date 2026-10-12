using web.Managers.People.Dtos;
using web.Repositories.People.Dtos;
using web.Repositories.PersonGroups.Dtos;

namespace web.ViewModels.Mappers;

/// <summary>Maps between the People view models and the People/PersonGroups repositories' and the People manager's Dtos.</summary>
public static class PeopleMapper
{
    public static SearchPeopleDto ToSearchPeopleDto(this PersonFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        GroupIds = filter.GroupIds,
        Type = filter.Type,
        Page = filter.Page,
        PageSize = filter.PageSize
    };

    public static PersonFilterViewModel ToPersonFilterViewModel(this PeopleOverviewDto overview, PersonFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        GroupIds = filter.GroupIds,
        Type = filter.Type,
        Page = overview.People.Page,
        PageSize = overview.People.PageSize,
        TotalCount = overview.People.TotalCount,
        People = overview.People.Items.Select(ToPersonListItemViewModel).ToList(),
        Groups = overview.Groups.Select(g => new PersonGroupOptionViewModel { Id = g.Id, Name = g.Name }).ToList()
    };

    public static PersonListItemViewModel ToPersonListItemViewModel(this PersonListItemDto person) => new()
    {
        Id = person.Id,
        Uid = person.Uid,
        Name = person.Name,
        BirthDate = person.BirthDate,
        Mobile = person.Mobile,
        Email = person.Email,
        Memberships = person.Memberships
            .Select(m => new PersonGroupMembershipItemViewModel { GroupId = m.GroupId, GroupName = m.GroupName, Type = m.Type })
            .ToList(),
        GuardianCount = person.GuardianCount
    };

    public static PersonDetailViewModel ToPersonDetailViewModel(this PersonDto person) => new()
    {
        Id = person.Id,
        Uid = person.Uid,
        Name = person.Name,
        BirthDate = person.BirthDate,
        Mobile = person.Mobile,
        Email = person.Email,
        GroupIds = person.Memberships.Select(m => m.GroupId).ToList(),
        GroupTypes = person.Memberships.ToDictionary(m => m.GroupId, m => m.Type),
        Guardians = person.Guardians
            .Select(g => new PersonGuardianViewModel { Id = g.Id, Name = g.Name, Mobile = g.Mobile, Email = g.Email })
            .ToList()
    };

    public static CreatePersonDto ToCreatePersonDto(this CreatePersonViewModel model) => new()
    {
        Uid = model.Uid,
        Name = model.Name,
        BirthDate = model.BirthDate,
        Mobile = model.Mobile,
        Email = model.Email,
        GroupIds = model.GroupIds,
        GroupTypes = model.GroupTypes,
        Guardians = model.Guardians.Select(ToPersonGuardianDto).ToList()
    };

    public static UpdatePersonDto ToUpdatePersonDto(this EditPersonViewModel model) => new()
    {
        Uid = model.Uid,
        Name = model.Name,
        BirthDate = model.BirthDate,
        Mobile = model.Mobile,
        Email = model.Email,
        GroupIds = model.GroupIds,
        GroupTypes = model.GroupTypes,
        Guardians = model.Guardians.Select(ToPersonGuardianDto).ToList()
    };

    public static PersonGroupViewModel ToPersonGroupViewModel(this PersonGroupDto group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        MemberCount = group.MemberCount,
        CreatedAtUtc = group.CreatedAt
    };

    private static PersonGuardianDto ToPersonGuardianDto(PersonGuardianViewModel guardian) => new()
    {
        Id = guardian.Id,
        Name = guardian.Name,
        Mobile = guardian.Mobile,
        Email = guardian.Email
    };
}
