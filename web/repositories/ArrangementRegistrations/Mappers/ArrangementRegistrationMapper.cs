using web.Data.Models;
using web.Repositories.ArrangementRegistrations.Dtos;

namespace web.Repositories.ArrangementRegistrations.Mappers;

public static class ArrangementRegistrationMapper
{
    public static ArrangementRegistration ToEntity(this CreateArrangementRegistrationDto dto, DateTime registeredAtUtc)
    {
        var registration = new ArrangementRegistration
        {
            ArrangementId = dto.ArrangementId,
            PersonId = dto.PersonId,
            RegisteredAtUtc = registeredAtUtc
        };

        foreach (var shift in dto.Shifts)
            registration.Shifts.Add(new ArrangementRegistrationShift { ArrangementShiftId = shift.ArrangementShiftId, CompanionName = shift.CompanionName });
        foreach (var answer in dto.Answers)
            registration.Answers.Add(new ArrangementRegistrationAnswer { ArrangementFormFieldId = answer.ArrangementFormFieldId, ValueText = answer.ValueText });

        return registration;
    }

    public static ArrangementRegistrationDto ToDto(this ArrangementRegistration entity) => new()
    {
        Id = entity.Id,
        PersonName = entity.Person.Name,
        RegisteredAtUtc = entity.RegisteredAtUtc,
        Answers = entity.Answers.ToDictionary(a => a.ArrangementFormFieldId, a => a.ValueText ?? string.Empty),
        Shifts = entity.Shifts.Select(s => new RegistrationShiftDto
        {
            ArrangementShiftId = s.ArrangementShiftId,
            Title = s.ArrangementShift.Title,
            StartUtc = s.ArrangementShift.StartUtc,
            EndUtc = s.ArrangementShift.EndUtc,
            CompanionName = s.CompanionName
        }).ToList()
    };
}
