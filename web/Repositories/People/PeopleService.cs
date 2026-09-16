using System.Globalization;
using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Data.Entities;
using web.Repositories.People.Dtos;
using web.Repositories.People.Interfaces;
using web.ViewModels;

namespace web.Repositories.People
{
    public class PeopleService : IPeopleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PeopleService> _logger;

        public PeopleService(ApplicationDbContext context, ILogger<PeopleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PersonFilterViewModel> GetPeopleAsync(PersonFilterViewModel filter, CancellationToken ct = default)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize is < 10 or > 500 ? 10 : filter.PageSize;

            var query = _context.People
                .Include(p => p.Memberships).ThenInclude(m => m.Group)
                .Include(p => p.Guardians)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var search = filter.SearchText.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    p.Uid.ToLower().Contains(search) ||
                    (p.Mobile != null && p.Mobile.ToLower().Contains(search)) ||
                    (p.Email != null && p.Email.ToLower().Contains(search)));
            }

            if (filter.GroupIds.Count > 0)
            {
                var groupIds = filter.GroupIds;
                query = query.Where(p => p.Memberships.Any(m => groupIds.Contains(m.GroupId)));
            }

            filter.TotalCount = await query.CountAsync(ct);

            filter.People = await query
                .OrderBy(p => p.Name)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new PersonListItemViewModel
                {
                    Id = p.Id,
                    Uid = p.Uid,
                    Name = p.Name,
                    BirthDate = p.BirthDate,
                    Mobile = p.Mobile,
                    Email = p.Email,
                    GroupNames = p.Memberships.Select(m => m.Group.Name).OrderBy(n => n).ToList(),
                    GuardianCount = p.Guardians.Count
                })
                .ToListAsync(ct);

            filter.Groups = await _context.PersonGroups
                .OrderBy(g => g.Name)
                .Select(g => new PersonGroupOptionViewModel { Id = g.Id, Name = g.Name })
                .ToListAsync(ct);

            return filter;
        }

        public async Task<PersonDetailViewModel?> GetPersonDetailAsync(int id, CancellationToken ct = default)
        {
            return await _context.People
                .Where(p => p.Id == id)
                .Select(p => new PersonDetailViewModel
                {
                    Id = p.Id,
                    Uid = p.Uid,
                    Name = p.Name,
                    BirthDate = p.BirthDate,
                    Mobile = p.Mobile,
                    Email = p.Email,
                    GroupIds = p.Memberships.Select(m => m.GroupId).ToList(),
                    Guardians = p.Guardians
                        .OrderBy(g => g.Order)
                        .Select(g => new PersonGuardianViewModel
                        {
                            Id = g.Id,
                            Name = g.Name,
                            Mobile = g.Mobile,
                            Email = g.Email
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<CreatePersonResponseDto> CreatePersonAsync(CreatePersonRequestDto dto, CancellationToken ct = default)
        {
            var uid = string.IsNullOrWhiteSpace(dto.Uid) ? await GenerateUidAsync(ct) : dto.Uid.Trim();

            if (await UidExistsAsync(uid, null, ct))
            {
                return new CreatePersonResponseDto
                {
                    Success = false,
                    ErrorMessage = $"UID '{uid}' er allerede i brug. Vælg et andet."
                };
            }

            var person = new Person
            {
                Uid = uid,
                Name = dto.Name.Trim(),
                BirthDate = dto.BirthDate,
                Mobile = string.IsNullOrWhiteSpace(dto.Mobile) ? null : dto.Mobile.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            ApplyGroups(person, dto.GroupIds);
            ApplyGuardians(person, dto.Guardians);

            _context.People.Add(person);

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Kunne ikke oprette person pga. formentlig UID-kollision ({Uid})", uid);
                return new CreatePersonResponseDto
                {
                    Success = false,
                    ErrorMessage = $"UID '{uid}' er allerede i brug. Vælg et andet."
                };
            }

            return new CreatePersonResponseDto { Success = true, PersonId = person.Id, GeneratedUid = uid };
        }

        public async Task<UpdatePersonResponseDto> UpdatePersonAsync(UpdatePersonRequestDto dto, CancellationToken ct = default)
        {
            var person = await _context.People
                .Include(p => p.Memberships)
                .Include(p => p.Guardians)
                .FirstOrDefaultAsync(p => p.Id == dto.Id, ct);

            if (person is null)
            {
                return new UpdatePersonResponseDto { Success = false, ErrorMessage = "Personen blev ikke fundet." };
            }

            var uid = string.IsNullOrWhiteSpace(dto.Uid) ? await GenerateUidAsync(ct) : dto.Uid.Trim();

            if (await UidExistsAsync(uid, person.Id, ct))
            {
                return new UpdatePersonResponseDto
                {
                    Success = false,
                    ErrorMessage = $"UID '{uid}' er allerede i brug. Vælg et andet."
                };
            }

            person.Uid = uid;
            person.Name = dto.Name.Trim();
            person.BirthDate = dto.BirthDate;
            person.Mobile = string.IsNullOrWhiteSpace(dto.Mobile) ? null : dto.Mobile.Trim();
            person.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
            person.UpdatedAtUtc = DateTime.UtcNow;

            // Replace group memberships and guardians wholesale — simple and correct for lists of this size.
            _context.PersonGroupMemberships.RemoveRange(person.Memberships);
            person.Memberships.Clear();
            ApplyGroups(person, dto.GroupIds);

            _context.PersonGuardians.RemoveRange(person.Guardians);
            person.Guardians.Clear();
            ApplyGuardians(person, dto.Guardians);

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Kunne ikke opdatere person {PersonId} pga. formentlig UID-kollision ({Uid})", dto.Id, uid);
                return new UpdatePersonResponseDto
                {
                    Success = false,
                    ErrorMessage = $"UID '{uid}' er allerede i brug. Vælg et andet."
                };
            }

            return new UpdatePersonResponseDto { Success = true, GeneratedUid = uid };
        }

        public async Task<bool> DeletePersonAsync(int id, CancellationToken ct = default)
        {
            var person = await _context.People.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (person is null)
                return false;

            _context.People.Remove(person);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<List<PersonGroupViewModel>> GetGroupsAsync(CancellationToken ct = default)
        {
            return await _context.PersonGroups
                .OrderBy(g => g.Name)
                .Select(g => new PersonGroupViewModel
                {
                    Id = g.Id,
                    Name = g.Name,
                    MemberCount = g.Memberships.Count,
                    CreatedAtUtc = g.CreatedAtUtc
                })
                .ToListAsync(ct);
        }

        public async Task<PersonGroupMutationResponseDto> CreateGroupAsync(string name, CancellationToken ct = default)
        {
            var trimmed = name.Trim();

            if (await _context.PersonGroups.AnyAsync(g => g.Name == trimmed, ct))
            {
                return new PersonGroupMutationResponseDto { Success = false, ErrorMessage = $"Gruppen '{trimmed}' findes allerede." };
            }

            var group = new PersonGroup { Name = trimmed, CreatedAtUtc = DateTime.UtcNow };
            _context.PersonGroups.Add(group);

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Kunne ikke oprette gruppe pga. formentlig navnekollision ({Name})", trimmed);
                return new PersonGroupMutationResponseDto { Success = false, ErrorMessage = $"Gruppen '{trimmed}' findes allerede." };
            }

            return new PersonGroupMutationResponseDto { Success = true, GroupId = group.Id };
        }

        public async Task<PersonGroupMutationResponseDto> UpdateGroupAsync(int id, string name, CancellationToken ct = default)
        {
            var group = await _context.PersonGroups.FirstOrDefaultAsync(g => g.Id == id, ct);
            if (group is null)
            {
                return new PersonGroupMutationResponseDto { Success = false, ErrorMessage = "Gruppen blev ikke fundet." };
            }

            var trimmed = name.Trim();

            if (await _context.PersonGroups.AnyAsync(g => g.Name == trimmed && g.Id != id, ct))
            {
                return new PersonGroupMutationResponseDto { Success = false, ErrorMessage = $"Gruppen '{trimmed}' findes allerede." };
            }

            group.Name = trimmed;
            group.UpdatedAtUtc = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Kunne ikke omdøbe gruppe {GroupId} pga. formentlig navnekollision ({Name})", id, trimmed);
                return new PersonGroupMutationResponseDto { Success = false, ErrorMessage = $"Gruppen '{trimmed}' findes allerede." };
            }

            return new PersonGroupMutationResponseDto { Success = true, GroupId = id };
        }

        public async Task<PersonGroupMutationResponseDto> DeleteGroupAsync(int id, CancellationToken ct = default)
        {
            var group = await _context.PersonGroups.FirstOrDefaultAsync(g => g.Id == id, ct);
            if (group is null)
            {
                return new PersonGroupMutationResponseDto { Success = false, ErrorMessage = "Gruppen blev ikke fundet." };
            }

            // Cascade delete only removes membership rows (configured in ApplicationDbContext) — persons are kept.
            _context.PersonGroups.Remove(group);
            await _context.SaveChangesAsync(ct);
            return new PersonGroupMutationResponseDto { Success = true, GroupId = id };
        }

        public async Task<ImportPersonsToGroupResponseDto> ImportPersonsToGroupAsync(int groupId, List<string> rawNames, CancellationToken ct = default)
        {
            var groupExists = await _context.PersonGroups.AnyAsync(g => g.Id == groupId, ct);
            if (!groupExists)
            {
                return new ImportPersonsToGroupResponseDto { Success = false, ErrorMessage = "Gruppen blev ikke fundet." };
            }

            var errors = new List<ImportPersonErrorDto>();
            var cleanedNames = new List<string>();

            foreach (var raw in rawNames)
            {
                var name = (raw ?? string.Empty).Trim().Trim(',').Trim();
                if (name.Length == 0)
                    continue;

                if (name.Length > 200)
                {
                    errors.Add(new ImportPersonErrorDto { Name = name, Reason = "Navnet er for langt (maks. 200 tegn)." });
                    continue;
                }

                cleanedNames.Add(ToTitleCaseName(name));
            }

            if (cleanedNames.Count == 0)
            {
                return new ImportPersonsToGroupResponseDto
                {
                    // No valid names survived cleaning: if per-line errors exist, report them (so the
                    // caller can show what's wrong); otherwise the input was entirely blank/empty, which
                    // is a top-level failure with no lines to point at.
                    Success = errors.Count > 0,
                    Errors = errors,
                    ErrorMessage = errors.Count == 0 ? "Angiv mindst ét navn." : null
                };
            }

            var nextUidNumber = await GetNextUidNumberAsync(ct);
            var people = new List<Person>();

            foreach (var name in cleanedNames)
            {
                var person = new Person
                {
                    Uid = nextUidNumber.ToString("D6"),
                    Name = name,
                    CreatedAtUtc = DateTime.UtcNow
                };
                person.Memberships.Add(new PersonGroupMembership { GroupId = groupId });
                people.Add(person);
                nextUidNumber++;
            }

            _context.People.AddRange(people);

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Kunne ikke importere personer til gruppe {GroupId}", groupId);
                return new ImportPersonsToGroupResponseDto { Success = false, ErrorMessage = "Importen kunne ikke gennemføres. Prøv igen." };
            }

            return new ImportPersonsToGroupResponseDto { Success = true, ImportedCount = people.Count, Errors = errors };
        }

        private static readonly CultureInfo DanishCulture = CultureInfo.GetCultureInfo("da-DK");

        private static string ToTitleCaseName(string name)
        {
            var words = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < words.Length; i++)
            {
                words[i] = DanishCulture.TextInfo.ToTitleCase(words[i].ToLower(DanishCulture));
            }

            return string.Join(' ', words);
        }

        private async Task<string> GenerateUidAsync(CancellationToken ct)
        {
            var next = await GetNextUidNumberAsync(ct);
            return next.ToString("D6");
        }

        private async Task<int> GetNextUidNumberAsync(CancellationToken ct)
        {
            var existingUids = await _context.People.Select(p => p.Uid).ToListAsync(ct);
            return existingUids
                .Select(uid => int.TryParse(uid, out var numeric) ? numeric : (int?)null)
                .Where(numeric => numeric.HasValue)
                .Select(numeric => numeric!.Value)
                .DefaultIfEmpty(0)
                .Max() + 1;
        }

        private async Task<bool> UidExistsAsync(string uid, int? excludePersonId, CancellationToken ct)
        {
            return await _context.People.AnyAsync(p => p.Uid == uid && (excludePersonId == null || p.Id != excludePersonId), ct);
        }

        private static void ApplyGroups(Person person, List<int> groupIds)
        {
            foreach (var groupId in groupIds.Distinct())
            {
                person.Memberships.Add(new PersonGroupMembership { GroupId = groupId });
            }
        }

        private static void ApplyGuardians(Person person, List<PersonGuardianViewModel> guardians)
        {
            var order = 0;
            foreach (var guardian in guardians.Where(g => !string.IsNullOrWhiteSpace(g.Name)))
            {
                person.Guardians.Add(new PersonGuardian
                {
                    Name = guardian.Name.Trim(),
                    Mobile = string.IsNullOrWhiteSpace(guardian.Mobile) ? null : guardian.Mobile.Trim(),
                    Email = string.IsNullOrWhiteSpace(guardian.Email) ? null : guardian.Email.Trim(),
                    Order = order++
                });
            }
        }
    }
}
