using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.PersonGroups.Dtos;

namespace web.Repositories.PersonGroups.Mappers;

public static class PersonGroupMapper
{
    public static readonly Expression<Func<PersonGroup, PersonGroupDto>> ToDtoExpression = x => new PersonGroupDto
    {
        Id = x.Id,
        Name = x.Name,
        MemberCount = x.Memberships.Count,
        CreatedAt = x.CreatedAt
    };

    private static readonly Func<PersonGroup, PersonGroupDto> CompiledToDto = ToDtoExpression.Compile();

    public static PersonGroupDto ToDto(this PersonGroup entity) => CompiledToDto(entity);

    public static PersonGroup ToEntity(string name) => new() { Name = name };
}
