using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Repositories.Registrations.Dtos;
using web.Repositories.Registrations.Interfaces;
using web.ViewModels;

namespace web.Repositories.Registrations
{
    public class ArrangementService : IArrangementService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ArrangementService> _logger;

        public ArrangementService(ApplicationDbContext context, ILogger<ArrangementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ArrangementFilterViewModel> GetArrangementsAsync(ArrangementFilterViewModel filter, CancellationToken ct = default)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize is < 5 or > 200 ? 10 : filter.PageSize;

            var query = _context.Arrangements.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var term = filter.SearchText.Trim();
                query = query.Where(a => a.Title.Contains(term));
            }

            filter.TotalCount = await query.CountAsync(ct);

            var page = await query
                .OrderByDescending(a => a.CreatedAtUtc)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    EventDateUtc = a.Shifts.Any() ? a.Shifts.Min(s => s.StartUtc) : (DateTime?)null,
                    NeededCount = a.Shifts.Sum(s => s.NeededCount),
                    a.RegistrationOpensAtUtc,
                    a.RegistrationClosesAtUtc,
                    a.CreatedAtUtc
                })
                .ToListAsync(ct);

            filter.Arrangementer = page
                .Select(a =>
                {
                    // TODO: erstat RegistrationCount med et rigtigt tilmeldingstal, når den offentlige
                    // tilmeldingsflow (og dermed en registrerings-tabel) er bygget. Mangler-tallet trækker
                    // allerede RegistrationCount fra, så det bliver korrekt automatisk den dag.
                    const int registrationCount = 0;
                    return new ArrangementListItemViewModel
                    {
                        Id = a.Id,
                        Title = a.Title,
                        EventDateUtc = a.EventDateUtc,
                        RegistrationCount = registrationCount,
                        MissingCount = Math.Max(0, a.NeededCount - registrationCount),
                        RegistrationOpensAtUtc = a.RegistrationOpensAtUtc,
                        RegistrationClosesAtUtc = a.RegistrationClosesAtUtc,
                        CreatedAtUtc = a.CreatedAtUtc
                    };
                })
                .ToList();

            return filter;
        }

        public async Task<ArrangementBuilderViewModel> GetBuilderShellAsync(CancellationToken ct = default)
        {
            return new ArrangementBuilderViewModel
            {
                GroupOptions = await GetGroupOptionsAsync(ct),
                PersonOptions = await GetPersonOptionsAsync(ct)
            };
        }

        public async Task<ArrangementBuilderViewModel?> GetArrangementForBuilderAsync(int id, CancellationToken ct = default)
        {
            var arrangement = await _context.Arrangements
                .AsNoTracking()
                .Include(a => a.FormFields)
                .Include(a => a.Shifts).ThenInclude(s => s.Requirements)
                .Include(a => a.AllowedPersons)
                .Include(a => a.AllowedGroups)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (arrangement is null) return null;

            return new ArrangementBuilderViewModel
            {
                Id = arrangement.Id,
                Title = arrangement.Title,
                Description = arrangement.Description,
                FormFields = arrangement.FormFields
                    .OrderBy(f => f.Order)
                    .Select(f => new ArrangementFormFieldBuilderViewModel
                    {
                        Id = f.Id,
                        Label = f.Label,
                        HelpText = f.HelpText,
                        FieldType = f.FieldType,
                        IsRequired = f.IsRequired,
                        Order = f.Order,
                        OptionsJson = f.OptionsJson
                    })
                    .ToList(),
                Shifts = arrangement.Shifts
                    .OrderBy(s => s.StartUtc)
                    .ThenBy(s => s.Order)
                    .Select(s => new ArrangementShiftBuilderViewModel
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Start = ToLocal(s.StartUtc),
                        End = ToLocal(s.EndUtc),
                        Location = s.Location,
                        NeededCount = s.NeededCount,
                        Order = s.Order,
                        Requirements = s.Requirements
                            .OrderBy(r => r.Order)
                            .Select(r => new ArrangementShiftRequirementBuilderViewModel
                            {
                                Id = r.Id,
                                Text = r.Text,
                                Order = r.Order
                            })
                            .ToList()
                    })
                    .ToList(),
                RegistrationOpensAt = ToLocal(arrangement.RegistrationOpensAtUtc),
                RegistrationClosesAt = ToLocal(arrangement.RegistrationClosesAtUtc),
                AccessMode = arrangement.AccessMode,
                AllowedPersonIds = arrangement.AllowedPersons.Select(p => p.PersonId).ToList(),
                AllowedGroupIds = arrangement.AllowedGroups.Select(g => g.PersonGroupId).ToList(),
                GroupOptions = await GetGroupOptionsAsync(ct),
                PersonOptions = await GetPersonOptionsAsync(ct)
            };
        }

        public async Task<SaveArrangementResponseDto> SaveArrangementAsync(SaveArrangementRequestDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return new SaveArrangementResponseDto { Success = false, ErrorMessage = "Arrangementet skal have en titel." };

            foreach (var shiftDto in dto.Shifts)
            {
                if (shiftDto.End <= shiftDto.Start)
                    return new SaveArrangementResponseDto { Success = false, ErrorMessage = $"'{shiftDto.Title}' skal have et sluttidspunkt efter starttidspunktet." };
            }

            Arrangement arrangement;
            if (dto.Id > 0)
            {
                var existing = await _context.Arrangements
                    .Include(a => a.FormFields)
                    .Include(a => a.Shifts).ThenInclude(s => s.Requirements)
                    .Include(a => a.AllowedPersons)
                    .Include(a => a.AllowedGroups)
                    .FirstOrDefaultAsync(a => a.Id == dto.Id, ct);

                if (existing is null)
                    return new SaveArrangementResponseDto { Success = false, ErrorMessage = "Arrangementet blev ikke fundet." };

                arrangement = existing;
                arrangement.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                arrangement = new Arrangement
                {
                    CreatedByUserId = dto.UserId,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _context.Arrangements.Add(arrangement);
            }

            arrangement.Title = dto.Title.Trim();
            arrangement.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            arrangement.RegistrationOpensAtUtc = ToUtc(dto.RegistrationOpensAtUtc);
            arrangement.RegistrationClosesAtUtc = ToUtc(dto.RegistrationClosesAtUtc);
            arrangement.AccessMode = dto.AccessMode;

            // a) Tilmeldingsformular — reconcile mod eksisterende felter ud fra Id.
            var existingFields = arrangement.FormFields.ToDictionary(f => f.Id);
            var keptFieldIds = new HashSet<int>();

            foreach (var fieldDto in dto.FormFields)
            {
                if (fieldDto.FieldType != FormFieldType.SectionHeading && string.IsNullOrWhiteSpace(fieldDto.Label))
                    return new SaveArrangementResponseDto { Success = false, ErrorMessage = "Alle spørgsmål i tilmeldingsformularen skal have en label." };

                string? optionsJson = null;
                if (FormFieldTypes.HasOptions(fieldDto.FieldType))
                {
                    var options = ParseOptions(fieldDto.OptionsJson);
                    if (options.Count == 0)
                        return new SaveArrangementResponseDto { Success = false, ErrorMessage = $"'{fieldDto.Label}' skal have mindst én mulighed." };

                    optionsJson = JsonSerializer.Serialize(options);
                }

                if (fieldDto.Id > 0 && existingFields.TryGetValue(fieldDto.Id, out var field))
                {
                    field.Label = fieldDto.Label.Trim();
                    field.HelpText = string.IsNullOrWhiteSpace(fieldDto.HelpText) ? null : fieldDto.HelpText.Trim();
                    field.FieldType = fieldDto.FieldType;
                    field.IsRequired = fieldDto.IsRequired;
                    field.Order = fieldDto.Order;
                    field.OptionsJson = optionsJson;
                    keptFieldIds.Add(field.Id);
                }
                else
                {
                    arrangement.FormFields.Add(new ArrangementFormField
                    {
                        Label = fieldDto.Label.Trim(),
                        HelpText = string.IsNullOrWhiteSpace(fieldDto.HelpText) ? null : fieldDto.HelpText.Trim(),
                        FieldType = fieldDto.FieldType,
                        IsRequired = fieldDto.IsRequired,
                        Order = fieldDto.Order,
                        OptionsJson = optionsJson
                    });
                }
            }

            foreach (var removed in existingFields.Values.Where(f => !keptFieldIds.Contains(f.Id)))
            {
                _context.ArrangementFormFields.Remove(removed);
            }

            // b) + c) Vagter, hver med sine egne krav — reconcile ét niveau, så et andet under.
            var existingShifts = arrangement.Shifts.ToDictionary(s => s.Id);
            var keptShiftIds = new HashSet<int>();

            foreach (var shiftDto in dto.Shifts)
            {
                if (string.IsNullOrWhiteSpace(shiftDto.Title))
                    return new SaveArrangementResponseDto { Success = false, ErrorMessage = "Alle vagter skal have en titel." };

                ArrangementShift shift;
                if (shiftDto.Id > 0 && existingShifts.TryGetValue(shiftDto.Id, out var existingShift))
                {
                    shift = existingShift;
                    keptShiftIds.Add(shift.Id);
                }
                else
                {
                    shift = new ArrangementShift();
                    arrangement.Shifts.Add(shift);
                }

                shift.Title = shiftDto.Title.Trim();
                shift.StartUtc = ToUtc(shiftDto.Start);
                shift.EndUtc = ToUtc(shiftDto.End);
                shift.Location = string.IsNullOrWhiteSpace(shiftDto.Location) ? null : shiftDto.Location.Trim();
                shift.NeededCount = shiftDto.NeededCount;
                shift.Order = shiftDto.Order;

                var existingRequirements = shift.Requirements.ToDictionary(r => r.Id);
                var keptRequirementIds = new HashSet<int>();

                foreach (var reqDto in shiftDto.Requirements)
                {
                    if (string.IsNullOrWhiteSpace(reqDto.Text))
                        continue;

                    if (reqDto.Id > 0 && existingRequirements.TryGetValue(reqDto.Id, out var requirement))
                    {
                        requirement.Text = reqDto.Text.Trim();
                        requirement.Order = reqDto.Order;
                        keptRequirementIds.Add(requirement.Id);
                    }
                    else
                    {
                        shift.Requirements.Add(new ArrangementShiftRequirement
                        {
                            Text = reqDto.Text.Trim(),
                            Order = reqDto.Order
                        });
                    }
                }

                foreach (var removedRequirement in existingRequirements.Values.Where(r => !keptRequirementIds.Contains(r.Id)))
                {
                    _context.ArrangementShiftRequirements.Remove(removedRequirement);
                }
            }

            foreach (var removedShift in existingShifts.Values.Where(s => !keptShiftIds.Contains(s.Id)))
            {
                _context.ArrangementShifts.Remove(removedShift);
            }

            // e) Adgang — ryd og gen-opbyg begge lister; kun relevante når adgangen er begrænset.
            _context.ArrangementAllowedPersons.RemoveRange(arrangement.AllowedPersons);
            arrangement.AllowedPersons.Clear();
            _context.ArrangementAllowedGroups.RemoveRange(arrangement.AllowedGroups);
            arrangement.AllowedGroups.Clear();

            if (dto.AccessMode == ArrangementAccessMode.Restricted)
            {
                foreach (var personId in dto.AllowedPersonIds.Distinct())
                {
                    arrangement.AllowedPersons.Add(new ArrangementAllowedPerson { PersonId = personId });
                }

                foreach (var groupId in dto.AllowedGroupIds.Distinct())
                {
                    arrangement.AllowedGroups.Add(new ArrangementAllowedGroup { PersonGroupId = groupId });
                }
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Arrangement {ArrangementId} saved with {FieldCount} formularfelter og {ShiftCount} vagter", arrangement.Id, dto.FormFields.Count, dto.Shifts.Count);
            return new SaveArrangementResponseDto { Success = true, ArrangementId = arrangement.Id };
        }

        public async Task<bool> DeleteArrangementAsync(int id, CancellationToken ct = default)
        {
            var arrangement = await _context.Arrangements.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (arrangement is null) return false;

            _context.Arrangements.Remove(arrangement);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Arrangement {ArrangementId} deleted", id);
            return true;
        }

        public async Task<List<PersonGroupOptionViewModel>> GetGroupOptionsAsync(CancellationToken ct = default)
        {
            return await _context.PersonGroups
                .OrderBy(g => g.Name)
                .Select(g => new PersonGroupOptionViewModel { Id = g.Id, Name = g.Name })
                .ToListAsync(ct);
        }

        public async Task<List<ArrangementPersonOptionViewModel>> GetPersonOptionsAsync(CancellationToken ct = default)
        {
            return await _context.People
                .OrderBy(p => p.Name)
                .Select(p => new ArrangementPersonOptionViewModel { Id = p.Id, Uid = p.Uid, Name = p.Name })
                .ToListAsync(ct);
        }

        /// <summary>
        /// Datofelterne i opsætningssiden (Detaljer-fanen, vagternes start/slut) er
        /// &lt;input type="datetime-local"&gt; og indeholder derfor administratorens lokale
        /// tid uden tidszone-info. Da serveren kører i samme tidszone som brugerne, konverteres
        /// den lokale værdi eksplicit til UTC her, før den gemmes — ellers ville den blive
        /// sammenlignet forkert mod DateTime.UtcNow (fx i Status-kolonnen).
        /// </summary>
        private static DateTime? ToUtc(DateTime? localValue)
            => localValue.HasValue ? DateTime.SpecifyKind(localValue.Value, DateTimeKind.Local).ToUniversalTime() : null;

        private static DateTime ToUtc(DateTime localValue)
            => DateTime.SpecifyKind(localValue, DateTimeKind.Local).ToUniversalTime();

        /// <summary>Modstykket til ToUtc — SQLite giver DateTime tilbage uden Kind, så den skal markeres som Utc, før ToLocalTime() konverterer korrekt.</summary>
        private static DateTime? ToLocal(DateTime? utcValue)
            => utcValue.HasValue ? DateTime.SpecifyKind(utcValue.Value, DateTimeKind.Utc).ToLocalTime() : null;

        private static DateTime ToLocal(DateTime utcValue)
            => DateTime.SpecifyKind(utcValue, DateTimeKind.Utc).ToLocalTime();

        private static List<string> ParseOptions(string? optionsJson)
        {
            if (string.IsNullOrWhiteSpace(optionsJson)) return new List<string>();

            try
            {
                var options = JsonSerializer.Deserialize<List<string>>(optionsJson) ?? new List<string>();
                return options
                    .Select(o => o?.Trim() ?? string.Empty)
                    .Where(o => !string.IsNullOrWhiteSpace(o))
                    .ToList();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }
    }
}
