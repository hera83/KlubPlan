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
                    RegistrationCount = a.Shifts.Sum(s => s.Registrations.Count),
                    a.RegistrationOpensAtUtc,
                    a.RegistrationClosesAtUtc,
                    a.RegistrationForcedOpen,
                    a.CreatedAtUtc
                })
                .ToListAsync(ct);

            filter.Arrangementer = page
                .Select(a => new ArrangementListItemViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    EventDateUtc = a.EventDateUtc,
                    RegistrationCount = a.RegistrationCount,
                    MissingCount = Math.Max(0, a.NeededCount - a.RegistrationCount),
                    RegistrationOpensAtUtc = a.RegistrationOpensAtUtc,
                    RegistrationClosesAtUtc = a.RegistrationClosesAtUtc,
                    RegistrationForcedOpen = a.RegistrationForcedOpen,
                    CreatedAtUtc = a.CreatedAtUtc
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
                AllowMultipleNamesPerShift = arrangement.AllowMultipleNamesPerShift,
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
            arrangement.AllowMultipleNamesPerShift = dto.AllowMultipleNamesPerShift;

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

        /// <summary>
        /// Flips RegistrationForcedOpen: closed → temporarily forced open (overriding the dates),
        /// forced open → back to following the configured dates. There is no forced-closed state —
        /// closing registration is done by editing the dates.
        /// </summary>
        public async Task<ToggleArrangementRegistrationResponseDto> ToggleRegistrationOpenAsync(int id, CancellationToken ct = default)
        {
            var arrangement = await _context.Arrangements.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (arrangement is null)
                return new ToggleArrangementRegistrationResponseDto { Success = false, ErrorMessage = "Arrangementet blev ikke fundet." };

            arrangement.RegistrationForcedOpen = !arrangement.RegistrationForcedOpen;
            arrangement.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Arrangement {ArrangementId} registration forced-open set to {ForcedOpen}", arrangement.Id, arrangement.RegistrationForcedOpen);
            return new ToggleArrangementRegistrationResponseDto { Success = true, IsOpen = arrangement.RegistrationForcedOpen };
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

        public async Task<PublicArrangementAccessViewModel> GetPublicArrangementAsync(Guid arrangementPublicId, Guid? personPublicId, CancellationToken ct = default)
        {
            var (status, arrangement, person) = await ResolvePublicArrangementAccessAsync(arrangementPublicId, personPublicId, ct);
            return await BuildPublicArrangementAccessViewModelAsync(status, arrangement, person, personPublicId, ct);
        }

        public async Task<SubmitPublicArrangementResponseDto> SubmitPublicArrangementAsync(SubmitPublicArrangementRequestDto dto, CancellationToken ct = default)
        {
            var (status, arrangement, person) = await ResolvePublicArrangementAccessAsync(dto.ArrangementPublicId, dto.PersonPublicId, ct);
            if (status != PublicArrangementStatus.Ok || arrangement is null || person is null)
                return new SubmitPublicArrangementResponseDto { Status = status };

            var (answers, fieldErrors) = BuildRegistrationAnswers(arrangement, dto.Answers);
            if (fieldErrors.Count > 0)
                return new SubmitPublicArrangementResponseDto { Status = PublicArrangementStatus.Ok, Success = false, FieldErrors = fieldErrors };

            var selectedShiftIds = dto.SelectedShiftIds.Distinct().ToList();
            var shiftsById = arrangement.Shifts.ToDictionary(s => s.Id);

            if (selectedShiftIds.Any(id => !shiftsById.ContainsKey(id)))
                return new SubmitPublicArrangementResponseDto { Status = PublicArrangementStatus.Ok, Success = false, ShiftError = "En eller flere valgte vagter findes ikke længere." };

            if (selectedShiftIds.Count == 0 && arrangement.Shifts.Count > 0)
                return new SubmitPublicArrangementResponseDto { Status = PublicArrangementStatus.Ok, Success = false, ShiftError = "Vælg mindst én vagt." };

            var registration = new ArrangementRegistration
            {
                ArrangementId = arrangement.Id,
                PersonId = person.Id,
                RegisteredAtUtc = DateTime.UtcNow
            };

            foreach (var shiftId in selectedShiftIds)
            {
                var shift = shiftsById[shiftId];

                // Companions are only honored when the arrangement allows it — ignore anything
                // posted otherwise, regardless of what the client sent. Once the registrant has
                // named anyone for a shift (the "+" flow), every named slot — including the one
                // pre-filled with their own name — replaces the implicit self row, since they may
                // have edited it to someone else entirely (e.g. a parent taking over the shift).
                var companionNames = arrangement.AllowMultipleNamesPerShift
                    ? dto.ShiftCompanions.Where(c => c.ShiftId == shiftId).Select(c => c.Name?.Trim()).ToList()
                    : new List<string?>();

                if (companionNames.Any(string.IsNullOrWhiteSpace))
                    return new SubmitPublicArrangementResponseDto { Status = PublicArrangementStatus.Ok, Success = false, ShiftError = $"Alle navne for '{shift.Title}' skal udfyldes." };

                var slotsRequested = companionNames.Count > 0 ? companionNames.Count : 1;

                // Re-check capacity and required confirmations here (not just client-side) — the
                // gate above only validates identity/access, not per-shift state, which can have
                // changed since the GET rendered the form.
                var takenCount = await _context.ArrangementRegistrationShifts.CountAsync(rs => rs.ArrangementShiftId == shiftId, ct);
                if (takenCount + slotsRequested > shift.NeededCount)
                {
                    var remaining = Math.Max(0, shift.NeededCount - takenCount);
                    return new SubmitPublicArrangementResponseDto { Status = PublicArrangementStatus.Ok, Success = false, ShiftError = $"'{shift.Title}' har kun {remaining} ledig(e) plads(er) tilbage." };
                }

                if (shift.Requirements.Count > 0 && shift.Requirements.Any(r => !dto.ConfirmedRequirementIds.Contains(r.Id)))
                    return new SubmitPublicArrangementResponseDto { Status = PublicArrangementStatus.Ok, Success = false, ShiftError = $"Du skal bekræfte alle krav for at tilmelde dig '{shift.Title}'." };

                if (companionNames.Count > 0)
                {
                    foreach (var name in companionNames)
                        registration.Shifts.Add(new ArrangementRegistrationShift { ArrangementShiftId = shiftId, CompanionName = name!.Trim() });
                }
                else
                {
                    registration.Shifts.Add(new ArrangementRegistrationShift { ArrangementShiftId = shiftId });
                }
            }

            foreach (var answer in answers)
                registration.Answers.Add(answer);

            _context.ArrangementRegistrations.Add(registration);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Person {PersonId} registered publicly for arrangement {ArrangementId}", person.Id, arrangement.Id);
            return new SubmitPublicArrangementResponseDto { Status = PublicArrangementStatus.Ok, Success = true, ArrangementTitle = arrangement.Title };
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

        /// <summary>
        /// Gate for the public, unauthenticated /Tilmelding link: resolves the arrangement and
        /// always requires the UId query parameter to resolve to a real Person — unlike
        /// Formularer, Tilmelding has no anonymous mode, since the whole point of signing up is
        /// knowing who is coming. Also enforces the access list (AllowedPersons/AllowedGroups)
        /// when AccessMode is Restricted, and blocks a Person who already registered.
        /// </summary>
        private async Task<(PublicArrangementStatus Status, Arrangement? Arrangement, Person? Person)> ResolvePublicArrangementAccessAsync(Guid arrangementPublicId, Guid? personPublicId, CancellationToken ct)
        {
            var arrangement = await _context.Arrangements
                .Include(a => a.FormFields)
                .Include(a => a.Shifts).ThenInclude(s => s.Requirements)
                .Include(a => a.AllowedPersons)
                .Include(a => a.AllowedGroups)
                .FirstOrDefaultAsync(a => a.PublicId == arrangementPublicId, ct);

            if (arrangement is null)
                return (PublicArrangementStatus.NotFound, null, null);

            var registrationStatus = ArrangementRegistrationStatuses.GetStatus(arrangement.RegistrationOpensAtUtc, arrangement.RegistrationClosesAtUtc, arrangement.RegistrationForcedOpen);
            if (registrationStatus == ArrangementRegistrationStatus.NotYetOpen)
                return (PublicArrangementStatus.NotYetOpen, arrangement, null);
            if (registrationStatus == ArrangementRegistrationStatus.Closed)
                return (PublicArrangementStatus.Closed, arrangement, null);

            if (personPublicId is null || personPublicId == Guid.Empty)
                return (PublicArrangementStatus.MissingIdentity, arrangement, null);

            var person = await _context.People.FirstOrDefaultAsync(p => p.PublicId == personPublicId.Value, ct);
            if (person is null)
                return (PublicArrangementStatus.InvalidIdentity, arrangement, null);

            if (arrangement.AccessMode == ArrangementAccessMode.Restricted)
            {
                var allowedPersonIds = arrangement.AllowedPersons.Select(p => p.PersonId).ToHashSet();
                var isDirectlyAllowed = allowedPersonIds.Contains(person.Id);

                var allowedGroupIds = arrangement.AllowedGroups.Select(g => g.PersonGroupId).ToList();
                var isAllowedViaGroup = allowedGroupIds.Count > 0 && await _context.PersonGroupMemberships
                    .AnyAsync(m => m.PersonId == person.Id && allowedGroupIds.Contains(m.GroupId), ct);

                if (!isDirectlyAllowed && !isAllowedViaGroup)
                    return (PublicArrangementStatus.NotAllowed, arrangement, person);
            }

            var alreadyRegistered = await _context.ArrangementRegistrations
                .AnyAsync(r => r.ArrangementId == arrangement.Id && r.PersonId == person.Id, ct);

            return alreadyRegistered
                ? (PublicArrangementStatus.AlreadyRegistered, arrangement, person)
                : (PublicArrangementStatus.Ok, arrangement, person);
        }

        private async Task<PublicArrangementAccessViewModel> BuildPublicArrangementAccessViewModelAsync(PublicArrangementStatus status, Arrangement? arrangement, Person? person, Guid? personPublicId, CancellationToken ct)
        {
            var vm = new PublicArrangementAccessViewModel { Status = status };
            if (arrangement is null) return vm;

            if (status != PublicArrangementStatus.Ok)
            {
                vm.Arrangement = new ArrangementFillViewModel
                {
                    ArrangementId = arrangement.Id,
                    ArrangementPublicId = arrangement.PublicId,
                    Title = arrangement.Title,
                    Description = arrangement.Description,
                    PersonPublicId = personPublicId,
                    RegistrationOpensAtUtc = arrangement.RegistrationOpensAtUtc
                };
                return vm;
            }

            var takenCountByShift = await _context.ArrangementRegistrationShifts
                .Where(rs => arrangement.Shifts.Select(s => s.Id).Contains(rs.ArrangementShiftId))
                .GroupBy(rs => rs.ArrangementShiftId)
                .Select(g => new { ArrangementShiftId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ArrangementShiftId, x => x.Count, ct);

            vm.Arrangement = new ArrangementFillViewModel
            {
                ArrangementId = arrangement.Id,
                ArrangementPublicId = arrangement.PublicId,
                Title = arrangement.Title,
                Description = arrangement.Description,
                PersonPublicId = personPublicId,
                PersonName = person?.Name,
                AllowMultipleNamesPerShift = arrangement.AllowMultipleNamesPerShift,
                Fields = arrangement.FormFields
                    .OrderBy(f => f.Order)
                    .Select(f => new ArrangementFieldFillViewModel
                    {
                        ArrangementFormFieldId = f.Id,
                        Label = f.Label,
                        HelpText = f.HelpText,
                        FieldType = f.FieldType,
                        IsRequired = f.IsRequired,
                        Options = ParseOptions(f.OptionsJson)
                    })
                    .ToList(),
                Shifts = arrangement.Shifts
                    .OrderBy(s => s.StartUtc)
                    .ThenBy(s => s.Order)
                    .Select(s => new ArrangementShiftFillViewModel
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Start = ToLocal(s.StartUtc),
                        End = ToLocal(s.EndUtc),
                        Location = s.Location,
                        RemainingCount = Math.Max(0, s.NeededCount - takenCountByShift.GetValueOrDefault(s.Id)),
                        Requirements = s.Requirements
                            .OrderBy(r => r.Order)
                            .Select(r => new ArrangementShiftRequirementFillViewModel { Id = r.Id, Text = r.Text })
                            .ToList()
                    })
                    .Where(s => s.RemainingCount > 0)
                    .ToList()
            };

            return vm;
        }

        public async Task<ArrangementRegistrationsViewModel?> GetArrangementRegistrationsAsync(int arrangementId, int page, int pageSize, CancellationToken ct = default)
        {
            var vm = await BuildRegistrationsShellAsync(arrangementId, ct);
            if (vm is null) return null;

            vm.Page = page < 1 ? 1 : page;
            vm.PageSize = pageSize is < 5 or > 500 ? 10 : pageSize;

            vm.TotalCount = await _context.ArrangementRegistrations.CountAsync(r => r.ArrangementId == arrangementId, ct);
            vm.Rows = await BuildRegistrationRowsAsync(arrangementId, vm.Page, vm.PageSize, ct);

            return vm;
        }

        public async Task<ArrangementRegistrationsViewModel?> GetAllArrangementRegistrationsForExportAsync(int arrangementId, CancellationToken ct = default)
        {
            var vm = await BuildRegistrationsShellAsync(arrangementId, ct);
            if (vm is null) return null;

            vm.TotalCount = await _context.ArrangementRegistrations.CountAsync(r => r.ArrangementId == arrangementId, ct);
            vm.Page = 1;
            vm.PageSize = vm.TotalCount == 0 ? 1 : vm.TotalCount;
            vm.Rows = await BuildRegistrationRowsAsync(arrangementId, 1, vm.PageSize, ct);

            return vm;
        }

        private async Task<ArrangementRegistrationsViewModel?> BuildRegistrationsShellAsync(int arrangementId, CancellationToken ct)
        {
            var arrangement = await _context.Arrangements
                .AsNoTracking()
                .Include(a => a.FormFields)
                .FirstOrDefaultAsync(a => a.Id == arrangementId, ct);

            if (arrangement is null) return null;

            return new ArrangementRegistrationsViewModel
            {
                ArrangementId = arrangement.Id,
                ArrangementTitle = arrangement.Title,
                Columns = arrangement.FormFields
                    .Where(f => FormFieldTypes.IsAnswerable(f.FieldType))
                    .OrderBy(f => f.Order)
                    .Select(f => new ArrangementRegistrationColumnViewModel { ArrangementFormFieldId = f.Id, Label = f.Label })
                    .ToList()
            };
        }

        private async Task<List<ArrangementRegistrationRowViewModel>> BuildRegistrationRowsAsync(int arrangementId, int page, int pageSize, CancellationToken ct)
        {
            var registrations = await _context.ArrangementRegistrations
                .AsNoTracking()
                .Where(r => r.ArrangementId == arrangementId)
                .OrderByDescending(r => r.RegisteredAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(r => r.Person)
                .Include(r => r.Answers)
                .Include(r => r.Shifts).ThenInclude(s => s.ArrangementShift)
                .ToListAsync(ct);

            return registrations.Select(r => new ArrangementRegistrationRowViewModel
            {
                RegistrationId = r.Id,
                PersonName = r.Person.Name,
                RegisteredAtUtc = r.RegisteredAtUtc,
                Answers = r.Answers.ToDictionary(a => a.ArrangementFormFieldId, a => a.ValueText ?? string.Empty),
                ShiftLabels = r.Shifts
                    .GroupBy(rs => rs.ArrangementShiftId)
                    .OrderBy(g => g.First().ArrangementShift.StartUtc)
                    .Select(g =>
                    {
                        var shift = g.First().ArrangementShift;
                        var label = $"{shift.Title} ({ToLocal(shift.StartUtc):dd/MM HH:mm}–{ToLocal(shift.EndUtc):HH:mm})";
                        var companionNames = g.Select(rs => rs.CompanionName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                        if (companionNames.Count == 0) return label;

                        // A null CompanionName row means the implicit registrant is still one of the
                        // slots — "+" reads as "plus these". Once every slot has been explicitly
                        // named (the registrant's own field was used or edited too), there's no
                        // implicit self left, so list the names as-is instead of "plus".
                        var hasImplicitSelf = g.Any(rs => rs.CompanionName is null);
                        return hasImplicitSelf ? $"{label} — + {string.Join(", ", companionNames)}" : $"{label} — {string.Join(", ", companionNames)}";
                    })
                    .ToList()
            }).ToList();
        }

        /// <summary>Validates and converts posted answers against an arrangement's answerable fields. Mirrors FormService.BuildSubmissionAnswers.</summary>
        private static (List<ArrangementRegistrationAnswer> Answers, Dictionary<int, string> FieldErrors) BuildRegistrationAnswers(Arrangement arrangement, List<SubmitArrangementAnswerDto> answers)
        {
            var answersByField = answers.ToDictionary(a => a.ArrangementFormFieldId);
            var fieldErrors = new Dictionary<int, string>();
            var result = new List<ArrangementRegistrationAnswer>();

            foreach (var field in arrangement.FormFields.Where(f => FormFieldTypes.IsAnswerable(f.FieldType)).OrderBy(f => f.Order))
            {
                answersByField.TryGetValue(field.Id, out var answer);
                var isCheckboxes = FormFieldTypes.AllowsMultipleValues(field.FieldType);

                var values = isCheckboxes
                    ? (answer?.Values ?? new List<string>()).Where(v => !string.IsNullOrWhiteSpace(v)).ToList()
                    : new List<string>();
                var singleValue = isCheckboxes ? null : answer?.Value?.Trim();

                var isEmpty = isCheckboxes ? values.Count == 0 : string.IsNullOrWhiteSpace(singleValue);

                if (field.IsRequired && isEmpty)
                {
                    fieldErrors[field.Id] = "Dette felt er påkrævet.";
                    continue;
                }

                var valueText = isCheckboxes ? (values.Count > 0 ? string.Join(", ", values) : null) : singleValue;
                result.Add(new ArrangementRegistrationAnswer { ArrangementFormFieldId = field.Id, ValueText = valueText });
            }

            return (result, fieldErrors);
        }
    }
}
