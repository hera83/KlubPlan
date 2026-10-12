using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Repositories.FormSubmissions.Dtos;
using web.Repositories.FormSubmissions.Interfaces;
using web.Repositories.FormSubmissions.Mappers;

namespace web.Repositories.FormSubmissions;

public class FormSubmissionRepository(WebDbContext dbContext) : IFormSubmissionRepository
{
    public async Task<int> CreateAsync(CreateFormSubmissionDto dto, CancellationToken cancellationToken = default)
    {
        var submission = dto.ToEntity(DateTime.UtcNow);
        dbContext.FormSubmissions.Add(submission);
        await dbContext.SaveChangesAsync(cancellationToken);
        return submission.Id;
    }

    public Task<int> CountAsync(int formId, CancellationToken cancellationToken = default) =>
        dbContext.FormSubmissions.CountAsync(s => s.FormId == formId, cancellationToken);

    public async Task<List<FormSubmissionDto>> GetPageAsync(int formId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var submissions = await dbContext.FormSubmissions.AsNoTracking()
            .Where(s => s.FormId == formId)
            .OrderByDescending(s => s.SubmittedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.Answers)
            .ToListAsync(cancellationToken);
        if (submissions.Count == 0)
            return [];

        var userIds = submissions.Select(s => s.SubmittedByUserId).OfType<string>().Distinct().ToList();
        var userNames = await dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        var personIds = submissions.Select(s => s.SubmittedByPersonId).OfType<int>().Distinct().ToList();
        var personNames = await dbContext.People.AsNoTracking()
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        return submissions.Select(s => s.ToDto(userNames, personNames)).ToList();
    }

    public Task<bool> HasPersonSubmittedAsync(int formId, int personId, CancellationToken cancellationToken = default) =>
        dbContext.FormSubmissions.AnyAsync(s => s.FormId == formId && s.SubmittedByPersonId == personId, cancellationToken);
}
