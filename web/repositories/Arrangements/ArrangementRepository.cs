using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Models;
using web.Repositories.Arrangements.Dtos;
using web.Repositories.Arrangements.Interfaces;
using web.Repositories.Arrangements.Mappers;
using web.Repositories.Forms.Mappers;

namespace web.Repositories.Arrangements;

public class ArrangementRepository(WebDbContext dbContext, ILogger<ArrangementRepository> logger) : IArrangementRepository
{
    private const int DefaultPageSize = 10;
    private const int MinPageSize = 5;
    private const int MaxPageSize = 200;

    public async Task<ArrangementPageDto> SearchAsync(SearchArrangementsDto search, CancellationToken cancellationToken = default)
    {
        var page = search.Page < 1 ? 1 : search.Page;
        var pageSize = search.PageSize is < MinPageSize or > MaxPageSize ? DefaultPageSize : search.PageSize;

        var query = dbContext.Arrangements.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var term = search.SearchText.Trim();
            query = query.Where(a => a.Title.Contains(term));
        }

        return new ArrangementPageDto
        {
            TotalCount = await query.CountAsync(cancellationToken),
            Items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ArrangementMapper.ToListItemDtoExpression)
                .ToListAsync(cancellationToken),
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ArrangementDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        (await WithEverything(dbContext.Arrangements.AsNoTracking())
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken))?.ToDto();

    public Task<List<ArrangementLinkOptionDto>> GetLinkOptionsAsync(CancellationToken cancellationToken = default) =>
        dbContext.Arrangements.AsNoTracking()
            .OrderBy(a => a.Title)
            .Select(a => new ArrangementLinkOptionDto
            {
                Id = a.Id,
                Title = a.Title,
                IsRestricted = a.AccessMode == ArrangementAccessMode.Restricted,
                AllowedPersonIds = a.AllowedPersons.Select(p => p.PersonId).ToList(),
                AllowedGroupIds = a.AllowedGroups.Select(g => g.PersonGroupId).ToList(),
                RegistrationOpensAtUtc = a.RegistrationOpensAtUtc,
                RegistrationClosesAtUtc = a.RegistrationClosesAtUtc,
                RegistrationForcedOpen = a.RegistrationForcedOpen
            })
            .ToListAsync(cancellationToken);

    public async Task<ArrangementDto?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        (await WithEverything(dbContext.Arrangements.AsNoTracking())
            .FirstOrDefaultAsync(a => a.PublicId == publicId, cancellationToken))?.ToDto();

    public async Task<SaveArrangementResultDto> SaveAsync(SaveArrangementDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return Invalid("Arrangementet skal have en titel.");

        foreach (var shiftDto in dto.Shifts)
        {
            if (shiftDto.EndUtc <= shiftDto.StartUtc)
                return Invalid($"'{shiftDto.Title}' skal have et sluttidspunkt efter starttidspunktet.");
        }

        Arrangement arrangement;
        if (dto.Id > 0)
        {
            var existing = await WithEverything(dbContext.Arrangements).FirstOrDefaultAsync(a => a.Id == dto.Id, cancellationToken);
            if (existing is null)
                return new SaveArrangementResultDto { Status = SaveArrangementStatus.NotFound };

            arrangement = existing;
            dbContext.Entry(arrangement).State = EntityState.Modified;
        }
        else
        {
            arrangement = dto.ToEntity();
            dbContext.Arrangements.Add(arrangement);
        }

        dto.ApplyTo(arrangement);

        // a) Tilmeldingsformular — reconcile against the existing fields by Id.
        var existingFields = arrangement.FormFields.ToDictionary(f => f.Id);
        var keptFieldIds = new HashSet<int>();

        foreach (var fieldDto in dto.FormFields)
        {
            if (fieldDto.FieldType != FormFieldType.SectionHeading && string.IsNullOrWhiteSpace(fieldDto.Label))
                return Invalid("Alle spørgsmål i tilmeldingsformularen skal have en label.");

            string? optionsJson = null;
            if (FormFieldTypes.HasOptions(fieldDto.FieldType))
            {
                var options = FormMapper.ParseOptions(fieldDto.OptionsJson);
                if (options.Count == 0)
                    return Invalid($"'{fieldDto.Label}' skal have mindst én mulighed.");

                optionsJson = JsonSerializer.Serialize(options);
            }

            if (fieldDto.Id > 0 && existingFields.TryGetValue(fieldDto.Id, out var field))
            {
                keptFieldIds.Add(field.Id);
            }
            else
            {
                field = new ArrangementFormField();
                arrangement.FormFields.Add(field);
            }

            fieldDto.ApplyTo(field, optionsJson);
        }

        foreach (var removed in existingFields.Values.Where(f => !keptFieldIds.Contains(f.Id)))
            dbContext.ArrangementFormFields.Remove(removed);

        // b) + c) Shifts, each with its own requirements — reconcile one level, then the one below.
        var existingShifts = arrangement.Shifts.ToDictionary(s => s.Id);
        var keptShiftIds = new HashSet<int>();

        foreach (var shiftDto in dto.Shifts)
        {
            if (string.IsNullOrWhiteSpace(shiftDto.Title))
                return Invalid("Alle vagter skal have en titel.");

            if (shiftDto.Id > 0 && existingShifts.TryGetValue(shiftDto.Id, out var shift))
            {
                keptShiftIds.Add(shift.Id);
            }
            else
            {
                shift = new ArrangementShift();
                arrangement.Shifts.Add(shift);
            }

            shiftDto.ApplyTo(shift);

            var existingRequirements = shift.Requirements.ToDictionary(r => r.Id);
            var keptRequirementIds = new HashSet<int>();

            foreach (var requirementDto in shiftDto.Requirements.Where(r => !string.IsNullOrWhiteSpace(r.Text)))
            {
                if (requirementDto.Id > 0 && existingRequirements.TryGetValue(requirementDto.Id, out var requirement))
                {
                    keptRequirementIds.Add(requirement.Id);
                }
                else
                {
                    requirement = new ArrangementShiftRequirement();
                    shift.Requirements.Add(requirement);
                }

                requirement.Text = requirementDto.Text.Trim();
                requirement.Order = requirementDto.Order;
            }

            foreach (var removedRequirement in existingRequirements.Values.Where(r => !keptRequirementIds.Contains(r.Id)))
                dbContext.ArrangementShiftRequirements.Remove(removedRequirement);
        }

        foreach (var removedShift in existingShifts.Values.Where(s => !keptShiftIds.Contains(s.Id)))
            dbContext.ArrangementShifts.Remove(removedShift);

        // e) Access — clear and rebuild both lists; they only matter when access is restricted.
        dbContext.ArrangementAllowedPersons.RemoveRange(arrangement.AllowedPersons);
        arrangement.AllowedPersons.Clear();
        dbContext.ArrangementAllowedGroups.RemoveRange(arrangement.AllowedGroups);
        arrangement.AllowedGroups.Clear();

        if (dto.AccessMode == ArrangementAccessMode.Restricted)
        {
            foreach (var personId in dto.AllowedPersonIds.Distinct())
                arrangement.AllowedPersons.Add(new ArrangementAllowedPerson { PersonId = personId });
            foreach (var groupId in dto.AllowedGroupIds.Distinct())
                arrangement.AllowedGroups.Add(new ArrangementAllowedGroup { PersonGroupId = groupId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Arrangement {ArrangementId} saved with {FieldCount} formularfelter og {ShiftCount} vagter", arrangement.Id, dto.FormFields.Count, dto.Shifts.Count);
        return new SaveArrangementResultDto { Status = SaveArrangementStatus.Saved, ArrangementId = arrangement.Id };
    }

    public async Task<bool?> ToggleRegistrationForcedOpenAsync(int id, CancellationToken cancellationToken = default)
    {
        var arrangement = await dbContext.Arrangements.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (arrangement is null)
            return null;

        arrangement.RegistrationForcedOpen = !arrangement.RegistrationForcedOpen;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Arrangement {ArrangementId} registration forced-open set to {ForcedOpen}", arrangement.Id, arrangement.RegistrationForcedOpen);
        return arrangement.RegistrationForcedOpen;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var arrangement = await dbContext.Arrangements.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (arrangement is null)
            return false;

        dbContext.Arrangements.Remove(arrangement);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Arrangement {ArrangementId} deleted", id);
        return true;
    }

    private static IQueryable<Arrangement> WithEverything(IQueryable<Arrangement> query) => query
        .Include(a => a.FormFields)
        .Include(a => a.Shifts).ThenInclude(s => s.Requirements)
        .Include(a => a.AllowedPersons)
        .Include(a => a.AllowedGroups);

    private static SaveArrangementResultDto Invalid(string error) => new() { Status = SaveArrangementStatus.Invalid, ErrorMessage = error };
}
