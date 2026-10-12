using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Repositories.ArrangementRegistrations.Dtos;
using web.Repositories.ArrangementRegistrations.Interfaces;
using web.Repositories.ArrangementRegistrations.Mappers;

namespace web.Repositories.ArrangementRegistrations;

public class ArrangementRegistrationRepository(WebDbContext dbContext) : IArrangementRegistrationRepository
{
    public async Task<int> CreateAsync(CreateArrangementRegistrationDto dto, CancellationToken cancellationToken = default)
    {
        var registration = dto.ToEntity(DateTime.UtcNow);
        dbContext.ArrangementRegistrations.Add(registration);
        await dbContext.SaveChangesAsync(cancellationToken);
        return registration.Id;
    }

    public Task<int> CountAsync(int arrangementId, CancellationToken cancellationToken = default) =>
        dbContext.ArrangementRegistrations.CountAsync(r => r.ArrangementId == arrangementId, cancellationToken);

    public async Task<List<ArrangementRegistrationDto>> GetPageAsync(int arrangementId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var registrations = await dbContext.ArrangementRegistrations.AsNoTracking()
            .Where(r => r.ArrangementId == arrangementId)
            .OrderByDescending(r => r.RegisteredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.Person)
            .Include(r => r.Answers)
            .Include(r => r.Shifts).ThenInclude(s => s.ArrangementShift)
            .ToListAsync(cancellationToken);

        return registrations.Select(r => r.ToDto()).ToList();
    }

    public Task<bool> ExistsForPersonAsync(int arrangementId, int personId, CancellationToken cancellationToken = default) =>
        dbContext.ArrangementRegistrations.AnyAsync(r => r.ArrangementId == arrangementId && r.PersonId == personId, cancellationToken);

    public Task<Dictionary<int, int>> GetTakenCountsAsync(IReadOnlyCollection<int> shiftIds, CancellationToken cancellationToken = default) =>
        dbContext.ArrangementRegistrationShifts.AsNoTracking()
            .Where(rs => shiftIds.Contains(rs.ArrangementShiftId))
            .GroupBy(rs => rs.ArrangementShiftId)
            .Select(g => new { ShiftId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ShiftId, x => x.Count, cancellationToken);
}
